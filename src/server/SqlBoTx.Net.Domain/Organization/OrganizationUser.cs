using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SqlBoTx.Net.Domain.Organization
{
    /// <summary>
    /// 问数系统用户
    /// </summary>
    [Description("问数系统用户")]
    public class OrganizationUser
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Description("主键自增ID")]
        public long Id { get; set; }

        /// <summary>
        /// 用户显示名称
        /// </summary>
        [Description("用户显示名称，如：张三")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 登录账号
        /// </summary>
        [Description("登录账号")]
        public string LoginAccount { get; set; } = string.Empty;

        /// <summary>
        /// 登录密码
        /// </summary>
        [Description("登录密码")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 用户邮箱
        /// </summary>
        [Description("用户邮箱")]
        public string? Email { get; set; }

        /// <summary>
        /// 系统用户
        /// </summary>
        [Description("系统用户")]
        public bool Admin { get; set; } = false;

        /// <summary>
        /// 所属组织ID
        /// </summary>
        [Description("所属组织ID")]
        public long OrganizationId { get; set; }

        /// <summary>
        /// 所属角色ID
        /// </summary>
        [Description("所属角色ID")]
        public long OrganizationRoleId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Description("账号是否启用")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 最后登录时间
        /// </summary>
        [Description("最后一次登录时间")]
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Description("账号创建时间")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 导航属性-所属组织
        /// </summary>
        public virtual Organization? Organization { get; set; }

        /// <summary>
        /// 导航属性-所属角色
        /// </summary>
        public virtual OrganizationRole? OrganizationRole { get; set; }
    }
}
