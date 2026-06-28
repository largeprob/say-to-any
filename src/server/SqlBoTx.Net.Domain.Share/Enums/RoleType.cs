using System.ComponentModel;

namespace SqlBoTx.Net.Domain.Share.Enums
{
    /// <summary>
    /// 角色类型
    /// </summary>
    public enum RoleType
    {
        /// <summary>
        /// 系统角色（不可删除）
        /// </summary>
        [Description("系统角色")]
        System = 0,

        /// <summary>
        /// 自定义角色
        /// </summary>
        [Description("自定义角色")]
        Custom = 1,
    }
}
