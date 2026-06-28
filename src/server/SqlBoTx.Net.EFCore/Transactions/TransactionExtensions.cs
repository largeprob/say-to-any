using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SqlBoTx.Net.EFCore.Transactions
{
    /// <summary>
    /// 事务扩展方法
    /// </summary>
    public static class TransactionExtensions
    {
        /// <summary>
        /// 注册带事务管理的服务（使用 DispatchProxy 实现 AOP）
        /// </summary>
        /// <typeparam name="TService">服务接口</typeparam>
        /// <typeparam name="TImplementation">服务实现</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="lifetime">服务生命周期</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddTransactionalService<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TService : class
            where TImplementation : class, TService
        {
            // 先注册实现类（作为内部服务）
            var implementationKey = $"{typeof(TImplementation).FullName}_Implementation";
            services.AddService<TImplementation>(lifetime, implementationKey);

            // 再注册接口（使用事务代理）
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<TService>(sp =>
                        TransactionProxyFactory.CreateTransactionalService<TService, TImplementation>(sp));
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped<TService>(sp =>
                        TransactionProxyFactory.CreateTransactionalService<TService, TImplementation>(sp));
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient<TService>(sp =>
                        TransactionProxyFactory.CreateTransactionalService<TService, TImplementation>(sp));
                    break;
            }

            return services;
        }

        /// <summary>
        /// 添加服务（内部方法）
        /// </summary>
        private static void AddService<TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            string? key = null)
            where TImplementation : class
        {
            var descriptor = new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime);
            if (!string.IsNullOrEmpty(key))
            {
                descriptor = new ServiceDescriptor(typeof(TImplementation), key, typeof(TImplementation), lifetime);
            }
            services.Add(descriptor);
        }
    }
}
