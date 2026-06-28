using System.ComponentModel;
using SqlBoTx.Net.Domain.Organization;

namespace SqlBoTx.Net.Domain.Auth
{
    [Description("刷新令牌")]
    public class RefreshToken
    {
        [Description("主键自增ID")]
        public long Id { get; set; }

        [Description("用户ID")]
        public long UserId { get; set; }

        [Description("令牌哈希值")]
        public string TokenHash { get; set; } = string.Empty;

        [Description("关联的JWT ID")]
        public string JwtId { get; set; } = string.Empty;

        [Description("是否已撤销")]
        public bool IsRevoked { get; set; } = false;

        [Description("过期时间")]
        public DateTime ExpiresAt { get; set; }

        [Description("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual OrganizationUser? User { get; set; }
    }
}
