namespace SqlBoTx.Net.Application.Contracts.Auth.Dtos
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }

    public record LoginResult(LoginResponse Response, string RefreshToken, DateTime RefreshTokenExpiresAt);
}
