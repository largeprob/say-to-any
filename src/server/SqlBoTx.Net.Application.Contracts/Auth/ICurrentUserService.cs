namespace SqlBoTx.Net.Application.Contracts.Auth
{
    /// <summary>
    /// 当前登录用户信息服务，可在任意层注入使用
    /// </summary>
    public interface ICurrentUserService
    {
        long? UserId { get; }
        string? UserName { get; }
        string? LoginAccount { get; }
        bool IsAdmin { get; }
        long? OrganizationId { get; }
    }
}
