using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SqlBoTx.Net.Application.Contracts.Auth;
using SqlBoTx.Net.Domain.Organization;
using SqlBoTx.Net.Share.Configuration;
using SqlBoTx.Net.Share.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SqlBoTx.Net.Application.Auth
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;

        public TokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public TokenResult GenerateAccessToken(OrganizationUser user)
        {
            var jti = Guid.CreateVersion7().ToString();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("LoginAccount", user.LoginAccount),
                new Claim("Admin", user.Admin.ToString()),
                new Claim("OrganizationId", user.OrganizationId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jti)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return new TokenResult(tokenString, jti, expires);
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        public string HashRefreshToken(string refreshToken)
        {
            return SecurityHelper.HashStr(refreshToken, "refresh_token_salt");
        }

        public DateTime GetRefreshTokenExpiration()
        {
            return DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        }
    }
}
