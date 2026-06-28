using SqlBoTx.Net.Application.Contracts.Auth.Dtos;

namespace SqlBoTx.Net.Application.Contracts.Auth
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(LoginRequest input);
        Task SendRegisterEmailCodeAsync(SendRegisterEmailCodeRequest input);
        Task<LoginResult> RegisterAsync(RegisterRequest input);
        Task<LoginResult> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(long userId);
    }
}
