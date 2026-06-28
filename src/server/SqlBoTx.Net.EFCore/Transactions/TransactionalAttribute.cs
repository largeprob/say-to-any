using System.Data;

namespace SqlBoTx.Net.EFCore.Transactions
{
    /// <summary>
    /// 标记方法需要在事务中执行
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class TransactionalAttribute : Attribute
    {
        /// <summary>
        /// 事务隔离级别
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        /// <summary>
        /// 超时时间（秒），默认60秒
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 是否在异常时回滚（默认true）
        /// </summary>
        public bool RollbackOnException { get; set; } = true;
    }
}
