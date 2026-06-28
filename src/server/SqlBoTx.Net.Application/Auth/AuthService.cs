using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SqlBoTx.Net.Application.Contracts.Auth;
using SqlBoTx.Net.Application.Contracts.Auth.Dtos;
using SqlBoTx.Net.Domain;
using SqlBoTx.Net.Domain.Auth;
using SqlBoTx.Net.Domain.Organization;
using SqlBoTx.Net.Domain.Share.Enums;
using SqlBoTx.Net.Share.Exceptions;
using SqlBoTx.Net.Share.Helpers;

namespace SqlBoTx.Net.Application.Auth
{
    public class AuthService : IAuthService
    {
        private const int RegisterCodeExpirationMinutes = 10;
        private const string RegisterCodeCachePrefix = "Auth:RegisterEmailCode:";
        private const string DefaultRegisterOrganizationName = "Say To Any";
        private const string DefaultRegisterRoleName = "普通用户";

        private readonly IOrganizationUserRepository _userRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationRoleRepository _organizationRoleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly OrganizationUserManager _organizationUserManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IOrganizationUserRepository userRepository,
            IOrganizationRepository organizationRepository,
            IOrganizationRoleRepository organizationRoleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            OrganizationUserManager organizationUserManager,
            IUnitOfWork unitOfWork,
            IMemoryCache memoryCache,
            IEmailSender emailSender,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _organizationRepository = organizationRepository;
            _organizationRoleRepository = organizationRoleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _organizationUserManager = organizationUserManager;
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<LoginResult> LoginAsync(LoginRequest input)
        {
            var users = await _userRepository.ListAsync(q =>
                q.Where(x => x.LoginAccount == input.LoginAccount));
            var user = users.FirstOrDefault();

            if (user == null)
                throw new BusinessException("Auth001", "用户名或密码错误");

            if (!user.IsActive)
                throw new BusinessException("Auth002", "账号已被禁用");

            var parts = user.Password.Split('|');
            if (parts.Length != 2)
                throw new BusinessException("Auth003", "密码格式异常");

            var hash = parts[0];
            var salt = parts[1];
            var computedHash = SecurityHelper.HashStr(input.Password!, salt);

            if (hash != computedHash)
                throw new BusinessException("Auth004", "用户名或密码错误");

            var accessTokenResult = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = _tokenService.HashRefreshToken(refreshToken),
                JwtId = accessTokenResult.JwtId,
                ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
                CreatedAt = DateTime.Now
            };

            await using (var uow = await _unitOfWork.BeginTransactionAsync())
            {
                await _refreshTokenRepository.InsertAsync(refreshTokenEntity);
                user.LastLoginDate = DateTime.Now;
                await _userRepository.UpdateAsync(user);
                await uow.CommitAsync();
            }

            var response = new LoginResponse
            {
                AccessToken = accessTokenResult.Token,
                AccessTokenExpiresAt = accessTokenResult.ExpiresAt,
                UserId = user.Id,
                UserName = user.UserName,
                IsAdmin = user.Admin
            };

            return new LoginResult(response, refreshToken, refreshTokenEntity.ExpiresAt);
        }

        public async Task SendRegisterEmailCodeAsync(SendRegisterEmailCodeRequest input)
        {
            var email = NormalizeEmail(input.Email);
            await EnsureEmailNotRegisteredAsync(email);
            EnsureRegisterEmailCodeCanBeSent(email);

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var expiresIn = TimeSpan.FromMinutes(RegisterCodeExpirationMinutes);
            var expiresAt = DateTime.Now.Add(expiresIn);

            _memoryCache.Set(
                GetRegisterCodeCacheKey(email),
                new RegisterEmailCodeCacheItem(code, expiresAt),
                expiresIn);

            await _emailSender.SendVerificationCodeAsync(email, code, expiresIn);
        }

        public async Task<LoginResult> RegisterAsync(RegisterRequest input)
        {
            var email = NormalizeEmail(input.Email);
            var password = input.Password?.Trim() ?? string.Empty;

            if (password.Length < 8)
                throw new BusinessException("Auth008", "密码长度至少为8位");

            ValidateRegisterEmailCode(email, input.VerificationCode);
            await EnsureEmailNotRegisteredAsync(email);

            LoginResult result;
            await using (var uow = await _unitOfWork.BeginTransactionAsync())
            {
                var (organization, role) = await EnsureRegistrationDefaultsAsync();
                var user = new OrganizationUser
                {
                    UserName = BuildDefaultUserName(email),
                    LoginAccount = email,
                    Email = email,
                    Password = password,
                    Admin = false,
                    OrganizationId = organization.Id,
                    OrganizationRoleId = role.Id,
                    IsActive = true,
                };

                user = await _organizationUserManager.CreateAsync(user);
                await _userRepository.InsterAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var accessTokenResult = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                var refreshTokenEntity = new RefreshToken
                {
                    UserId = user.Id,
                    TokenHash = _tokenService.HashRefreshToken(refreshToken),
                    JwtId = accessTokenResult.JwtId,
                    ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
                    CreatedAt = DateTime.Now
                };

                await _refreshTokenRepository.InsertAsync(refreshTokenEntity);
                user.LastLoginDate = DateTime.Now;
                await _userRepository.UpdateAsync(user);
                await uow.CommitAsync();

                var response = new LoginResponse
                {
                    AccessToken = accessTokenResult.Token,
                    AccessTokenExpiresAt = accessTokenResult.ExpiresAt,
                    UserId = user.Id,
                    UserName = user.UserName,
                    IsAdmin = user.Admin
                };

                result = new LoginResult(response, refreshToken, refreshTokenEntity.ExpiresAt);
            }

            _memoryCache.Remove(GetRegisterCodeCacheKey(email));
            _logger.LogInformation("用户 {Email} 注册成功并已登录", email);
            return result;
        }

        public async Task<LoginResult> RefreshTokenAsync(string refreshToken)
        {
            var tokenHash = _tokenService.HashRefreshToken(refreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (storedToken == null)
                throw new BusinessException("Auth005", "无效的刷新令牌");

            if (storedToken.ExpiresAt < DateTime.Now)
                throw new BusinessException("Auth006", "刷新令牌已过期");

            var user = await _userRepository.GetByIdAsync(storedToken.UserId);
            if (user == null || !user.IsActive)
                throw new BusinessException("Auth007", "用户不存在或已被禁用");

            storedToken.IsRevoked = true;

            var newAccessTokenResult = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = _tokenService.HashRefreshToken(newRefreshToken),
                JwtId = newAccessTokenResult.JwtId,
                ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
                CreatedAt = DateTime.Now
            };

            await using (var uow = await _unitOfWork.BeginTransactionAsync())
            {
                await _refreshTokenRepository.UpdateAsync(storedToken);
                await _refreshTokenRepository.InsertAsync(newRefreshTokenEntity);
                await uow.CommitAsync();
            }

            var response = new LoginResponse
            {
                AccessToken = newAccessTokenResult.Token,
                AccessTokenExpiresAt = newAccessTokenResult.ExpiresAt,
                UserId = user.Id,
                UserName = user.UserName,
                IsAdmin = user.Admin
            };

            return new LoginResult(response, newRefreshToken, newRefreshTokenEntity.ExpiresAt);
        }

        public async Task LogoutAsync(long userId)
        {
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
        }

        private async Task EnsureEmailNotRegisteredAsync(string email)
        {
            var users = await _userRepository.ListAsync(q =>
                q.Where(x => x.LoginAccount == email || x.Email == email));

            if (users.Count > 0)
                throw new BusinessException("Auth009", "该邮箱已注册");
        }

        private void EnsureRegisterEmailCodeCanBeSent(string email)
        {
            var cacheKey = GetRegisterCodeCacheKey(email);
            if (!_memoryCache.TryGetValue<RegisterEmailCodeCacheItem>(cacheKey, out var item))
            {
                return;
            }

            if (item.ExpiresAt > DateTime.Now)
            {
                throw new BusinessException("Auth014", "验证码邮件已发送，请稍后再试");
            }

            _memoryCache.Remove(cacheKey);
        }

        private void ValidateRegisterEmailCode(string email, string? verificationCode)
        {
            var code = verificationCode?.Trim();
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException("Auth010", "邮箱验证码不能为空");

            if (!_memoryCache.TryGetValue<RegisterEmailCodeCacheItem>(GetRegisterCodeCacheKey(email), out var item)
                || item.ExpiresAt < DateTime.Now)
            {
                throw new BusinessException("Auth011", "邮箱验证码已过期，请重新发送");
            }

            if (!string.Equals(item.Code, code, StringComparison.Ordinal))
                throw new BusinessException("Auth012", "邮箱验证码不正确");
        }

        private async Task<(Organization Organization, OrganizationRole Role)> EnsureRegistrationDefaultsAsync()
        {
            var organization = (await _organizationRepository.ListAsync(q =>
                q.Where(x => x.Name == DefaultRegisterOrganizationName).Take(1))).FirstOrDefault();

            if (organization == null)
            {
                organization = new Organization
                {
                    Name = DefaultRegisterOrganizationName,
                    Description = "Say To Any 默认组织"
                };
                await _organizationRepository.InsterAsync(organization);
                await _unitOfWork.SaveChangesAsync();
            }

            var role = (await _organizationRoleRepository.ListAsync(q =>
                q.Where(x => x.Name == DefaultRegisterRoleName).Take(1))).FirstOrDefault();

            if (role == null)
            {
                role = new OrganizationRole
                {
                    Name = DefaultRegisterRoleName,
                    Description = "注册用户默认角色",
                    SortOrder = 100,
                    IsActive = true,
                    RoleType = RoleType.Custom,
                    CreatedDate = DateTime.Now
                };
                await _organizationRoleRepository.InsterAsync(role);
                await _unitOfWork.SaveChangesAsync();
            }

            return (organization, role);
        }

        private static string NormalizeEmail(string? email)
        {
            var normalized = email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized) || !new EmailAddressAttribute().IsValid(normalized))
                throw new BusinessException("Auth013", "邮箱格式不正确");

            return normalized;
        }

        private static string BuildDefaultUserName(string email)
        {
            var atIndex = email.IndexOf('@');
            return atIndex > 0 ? email[..atIndex] : email;
        }

        private static string GetRegisterCodeCacheKey(string email)
        {
            return $"{RegisterCodeCachePrefix}{email}";
        }

        private record RegisterEmailCodeCacheItem(string Code, DateTime ExpiresAt);
    }
}
