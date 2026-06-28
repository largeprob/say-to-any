namespace SqlBoTx.Net.ApiService.Auth
{
    public static class CookieHelper
    {
        private const string CookieName = "refresh_token";
        private const string CookiePath = "/";

        public static void AppendRefreshTokenCookie(HttpContext context, string token, DateTime expires)
        {
            context.Response.Cookies.Append(CookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = context.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = CookiePath,
                Expires = expires
            });
        }

        public static void DeleteRefreshTokenCookie(HttpContext context)
        {
            context.Response.Cookies.Delete(CookieName, new CookieOptions { Path = CookiePath });
        }

        public static string? ReadRefreshTokenFromCookie(HttpContext context)
        {
            context.Request.Cookies.TryGetValue(CookieName, out var token);
            return token;
        }
    }
}
