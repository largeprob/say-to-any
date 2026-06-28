using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace SqlBoTx.Net.EFCore.Transactions
{
    /// <summary>
    /// 事务代理工厂 - 使用 DispatchProxy 实现 AOP
    /// </summary>
    public static class TransactionProxyFactory
    {
        /// <summary>
        /// 创建带事务管理的服务代理
        /// </summary>
        public static TService CreateTransactionalService<TService, TImplementation>(
            IServiceProvider serviceProvider)
            where TService : class
            where TImplementation : class, TService
        {
            var implementation = ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider);
            return CreateTransactionalService<TService>(implementation, serviceProvider);
        }

        /// <summary>
        /// 创建带事务管理的服务代理
        /// </summary>
        public static TService CreateTransactionalService<TService>(
            TService target,
            IServiceProvider serviceProvider)
            where TService : class
        {
            var proxy = TransactionProxy<TService>.Create(target, serviceProvider);
            return proxy;
        }
    }

    /// <summary>
    /// 事务代理（使用 DispatchProxy 实现 AOP）
    /// </summary>
    /// <typeparam name="TService">服务接口</typeparam>
    public class TransactionProxy<TService> : DispatchProxy where TService : class
    {
        private TService? _target;
        private IServiceProvider? _serviceProvider;
        private ILogger<TransactionProxy<TService>>? _logger;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (_target == null || targetMethod == null || args == null)
                return null;

            var transactionalAttr = targetMethod.GetCustomAttribute<TransactionalAttribute>();

            if (transactionalAttr == null)
            {
                // 没有事务标记，直接执行
                return targetMethod.Invoke(_target, args);
            }

            // 执行带事务的方法
            return ExecuteWithTransactionAsync(targetMethod, args!, transactionalAttr).GetAwaiter().GetResult();
        }

        private async Task<object?> ExecuteWithTransactionAsync(
            MethodInfo targetMethod,
            object[] args,
            TransactionalAttribute transactionalAttr)
        {
            // 通过反射获取 IUnitOfWork（避免直接依赖）
            var unitOfWorkType = Type.GetType("SqlBoTx.Net.Domain.IUnitOfWork, SqlBoTx.Net.Domain");
            if (unitOfWorkType == null)
            {
                throw new InvalidOperationException("IUnitOfWork type not found. Please ensure SqlBoTx.Net.Domain is referenced.");
            }

            var unitOfWork = _serviceProvider!.GetService(unitOfWorkType);
            if (unitOfWork == null)
            {
                throw new InvalidOperationException("IUnitOfWork not registered in DI container.");
            }

            try
            {
                // 开始事务: BeginTransactionAsync(IsolationLevel)
                var beginMethod = unitOfWorkType.GetMethod("BeginTransactionAsync", new[] { typeof(System.Data.IsolationLevel) });
                if (beginMethod == null)
                {
                    throw new InvalidOperationException("BeginTransactionAsync method not found on IUnitOfWork.");
                }

                await (Task)beginMethod.Invoke(unitOfWork, new object[] { transactionalAttr.IsolationLevel })!;
                _logger?.LogInformation("开始事务: {MethodName}, 隔离级别: {IsolationLevel}",
                    targetMethod.Name, transactionalAttr.IsolationLevel);

                // 执行方法
                var result = targetMethod.Invoke(_target, args);

                // 处理异步结果
                if (result is Task task)
                {
                    await task;
                }

                // 提交事务: CommitAsync()
                var commitMethod = unitOfWorkType.GetMethod("CommitAsync");
                if (commitMethod != null)
                {
                    await (Task)commitMethod.Invoke(unitOfWork, null)!;
                }

                _logger?.LogInformation("事务已提交: {MethodName}", targetMethod.Name);

                return result;
            }
            catch (Exception ex)
            {
                // 回滚事务: RollbackAsync()
                var rollbackMethod = unitOfWorkType.GetMethod("RollbackAsync");
                if (rollbackMethod != null)
                {
                    try
                    {
                        await (Task)rollbackMethod.Invoke(unitOfWork, null)!;
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger?.LogError(rollbackEx, "事务回滚失败: {MethodName}", targetMethod.Name);
                    }
                }

                var logLevel = ex.GetType().Name == "BusinessException" ? LogLevel.Warning : LogLevel.Error;
                _logger?.Log(logLevel, ex, "事务回滚: {MethodName}, 错误: {ErrorMessage}",
                    targetMethod.Name, ex.Message);

                // 重新抛出异常
                throw;
            }
        }

        public static TService Create(TService target, IServiceProvider serviceProvider)
        {
            var proxy = Create<TService, TransactionProxy<TService>>() as TransactionProxy<TService>;
            if (proxy == null)
                throw new InvalidOperationException("Failed to create transaction proxy");

            proxy._target = target;
            proxy._serviceProvider = serviceProvider;
            proxy._logger = serviceProvider.GetService<ILogger<TransactionProxy<TService>>>();

            return proxy as TService;
        }
    }
}
