using System.Data;

namespace SqlBoTx.Net.Domain
{
    /// <summary>
    /// 事务单元接口
    /// </summary>
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task<IUnitOfWork> BeginTransactionAsync();
        Task<IUnitOfWork> BeginTransactionAsync(IsolationLevel isolationLevel);
        Task CommitAsync();
        Task RollbackAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
