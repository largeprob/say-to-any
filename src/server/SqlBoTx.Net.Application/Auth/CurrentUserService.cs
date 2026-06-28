using Microsoft.AspNetCore.Http;
using SqlBoTx.Net.Application.Contracts.Auth;
using System.Security.Claims;

namespace SqlBoTx.Net.Application.Auth
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public long? UserId =>
            long.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public string? UserName =>
            User?.FindFirstValue(ClaimTypes.Name);

        public string? LoginAccount =>
            User?.FindFirstValue("LoginAccount");

        public bool IsAdmin =>
            bool.TryParse(User?.FindFirstValue("Admin"), out var admin) && admin;

        public long? OrganizationId =>
            long.TryParse(User?.FindFirstValue("OrganizationId"), out var orgId) ? orgId : null;
    }
}
