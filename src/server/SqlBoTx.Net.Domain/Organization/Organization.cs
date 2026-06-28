using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SqlBoTx.Net.Domain.Organization
{
    /// <summary>
    /// 组织结构
    /// </summary>
    [Description("组织结构")]
    public class Organization
    {
        /// <summary>
        /// ID
        /// </summary>
        [Description("ID")]
        public long Id { get; set; }

        /// <summary>
        /// 组织名称
        /// </summary>
        [Description("组织名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 父组织ID
        /// </summary>
        [Description("父组织ID")]
        public long? ParentId { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string? Description { get; set; }

        public virtual Organization? Parent { get; set; }

        public virtual ICollection<Organization>? Children { get; set; }

        public virtual ICollection<OrganizationUser>? Users { get; set; }
    }
}
