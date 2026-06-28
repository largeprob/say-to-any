using System;
using System.Collections.Generic;
using System.ComponentModel;
using SqlBoTx.Net.Domain.Share.Enums;

namespace SqlBoTx.Net.Domain.Organization
{
    /// <summary>
    /// 组织角色
    /// </summary>
    [Description("组织角色")]
    public class OrganizationRole
    {
        /// <summary>
        /// 主键自增ID
        /// </summary>
        [Description("主键自增ID")]
        public long Id { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        [Description("角色名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 角色说明
        /// </summary>
        [Description("角色说明")]
        public string? Description { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        [Description("排序号")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 是否启用
        /// </summary>
        [Description("是否启用")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 角色类型
        /// </summary>
        [Description("角色类型")]
        public RoleType RoleType { get; set; } = RoleType.Custom;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Description("创建时间")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 导航属性-该角色下的用户
        /// </summary>
        public virtual ICollection<OrganizationUser>? Users { get; set; }
    }
}
