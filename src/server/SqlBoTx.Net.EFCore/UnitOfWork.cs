using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SqlBoTx.Net.Domain;
using System.Data;

namespace SqlBoTx.Net.EFCore
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SqlBotxDBContext _dbContext;
        public UnitOfWork(SqlBotxDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        private IDbContextTransaction? _currentTransaction;
        private bool _isCommitted;
        private bool _isDisposed;

        public async Task<IUnitOfWork> BeginTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            _currentTransaction = await _dbContext.Database.BeginTransactionAsync();
            _isCommitted = false;
            return this;
        }

        public async Task<IUnitOfWork> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(isolationLevel);
            _isCommitted = false;
            return this;
        }

        public async Task CommitAsync()
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("Transaction not started");
            }
            try
            {
                await _dbContext.SaveChangesAsync();
                await _currentTransaction.CommitAsync();
                _currentTransaction = null;
                _isCommitted = true;
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
                _isCommitted = true;
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _dbContext.SaveChangesAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                if (!_isCommitted && _currentTransaction != null)
                {
                    await RollbackAsync();
                }

                _isDisposed = true;
            }
        }
    }
}
