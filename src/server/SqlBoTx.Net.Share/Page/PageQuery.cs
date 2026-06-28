using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBoTx.Net.Share.Page
{
    public class PageQuery
    {
        /// <summary>
        /// 页码
        /// </summary>
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// 一页显示多少条
        /// </summary>
        [DefaultValue(30)]
        public int Limit { get; set; } = 30;

        /// <summary>
        /// 搜索值
        /// </summary>
        [DefaultValue("")]
        public string SearchValue { get; set; } = string.Empty;
    }
}
