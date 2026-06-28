using SqlBoTx.Net.Domain.Organization;

namespace SqlBoTx.Net.Application.Contracts.Auth
{
    public record TokenResult(string Token, string JwtId, DateTime ExpiresAt);

    public interface ITokenService
    {
        TokenResult GenerateAccessToken(OrganizationUser user);
        string GenerateRefreshToken();
        string HashRefreshToken(string refreshToken);
        DateTime GetRefreshTokenExpiration();
    }
}
