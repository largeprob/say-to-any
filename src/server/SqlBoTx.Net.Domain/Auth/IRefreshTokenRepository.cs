namespace SqlBoTx.Net.Domain.Auth
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task<List<RefreshToken>> GetActiveByUserIdAsync(long userId);
        Task InsertAsync(RefreshToken entity);
        Task UpdateAsync(RefreshToken entity);
        Task RevokeAllByUserIdAsync(long userId);
    }
}
