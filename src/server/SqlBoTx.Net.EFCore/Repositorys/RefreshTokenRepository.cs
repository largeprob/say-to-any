using Microsoft.EntityFrameworkCore;
using SqlBoTx.Net.Domain.Auth;

namespace SqlBoTx.Net.EFCore.Repositorys
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly SqlBotxDBContext _dbContext;

        public RefreshTokenRepository(SqlBotxDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _dbContext.Set<RefreshToken>()
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && !x.IsRevoked);
        }

        public async Task<List<RefreshToken>> GetActiveByUserIdAsync(long userId)
        {
            return await _dbContext.Set<RefreshToken>()
                .Where(x => x.UserId == userId && !x.IsRevoked && x.ExpiresAt > DateTime.Now)
                .ToListAsync();
        }

        public async Task InsertAsync(RefreshToken entity)
        {
            await _dbContext.Set<RefreshToken>().AddAsync(entity);
        }

        public async Task UpdateAsync(RefreshToken entity)
        {
            var existing = await _dbContext.Set<RefreshToken>()
                .FirstAsync(x => x.Id == entity.Id);
            _dbContext.Entry(existing).CurrentValues.SetValues(entity);
        }

        public async Task RevokeAllByUserIdAsync(long userId)
        {
            await _dbContext.Set<RefreshToken>()
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRevoked, true));
        }
    }
}
