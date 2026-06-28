using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlBoTx.Net.ApiService.Auth;
using SqlBoTx.Net.Application.Contracts.Auth;
using SqlBoTx.Net.Application.Contracts.Auth.Dtos;
using SqlBoTx.Net.Core.Controller;

namespace SqlBoTx.Net.ApiService.Controllers
{
    /// <summary>
    /// 认证管理
    /// </summary>
    [Route("[controller]")]
    [Consumes("application/json")]
    [Tags("认证管理")]
    public class AuthController : LarApi
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUserService;

        public AuthController(IAuthService authService, ICurrentUserService currentUserService)
        {
            _authService = authService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// 登录
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(LoginResponse))]
        public async Task<IActionResult> Login([FromBody] LoginRequest input)
        {
            var result = await _authService.LoginAsync(input);
            CookieHelper.AppendRefreshTokenCookie(HttpContext, result.RefreshToken, result.RefreshTokenExpiresAt);
            return Ok(result.Response);
        }

        /// <summary>
        /// 发送注册邮箱验证码
        /// </summary>
        [HttpPost("register/send-email-code")]
        [AllowAnonymous]
        [ProducesResponseType(204)]
        public async Task<IActionResult> SendRegisterEmailCode([FromBody] SendRegisterEmailCodeRequest input)
        {
            await _authService.SendRegisterEmailCodeAsync(input);
            return NoContent();
        }

        /// <summary>
        /// 邮箱验证码注册，注册成功后直接登录
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(LoginResponse))]
        public async Task<IActionResult> Register([FromBody] RegisterRequest input)
        {
            var result = await _authService.RegisterAsync(input);
            CookieHelper.AppendRefreshTokenCookie(HttpContext, result.RefreshToken, result.RefreshTokenExpiresAt);
            return Ok(result.Response);
        }

        /// <summary>
        /// 刷新令牌
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(LoginResponse))]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = CookieHelper.ReadRefreshTokenFromCookie(HttpContext);
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized();

            var result = await _authService.RefreshTokenAsync(refreshToken);
            CookieHelper.AppendRefreshTokenCookie(HttpContext, result.RefreshToken, result.RefreshTokenExpiresAt);
            return Ok(result.Response);
        }

        /// <summary>
        /// 登出
        /// </summary>
        [HttpPost("logout")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> Logout()
        {
            var userId = _currentUserService.UserId!.Value;
            await _authService.LogoutAsync(userId);
            CookieHelper.DeleteRefreshTokenCookie(HttpContext);
            return NoContent();
        }
    }
}
