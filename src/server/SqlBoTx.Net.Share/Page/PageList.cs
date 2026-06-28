using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBoTx.Net.Share.Page
{
    public class PageList<T>
    {
        /// <summary>
        /// 总页数
        /// </summary>
        public long TotalPage { get; set; }

        /// <summary>
        /// 总数
        /// </summary>
        public long TotalRow { get; set; }

        /// <summary>
        /// 返回值
        /// </summary>
        public IEnumerable<T>? Item { get; set; }
    }

    public class PageWatchList<T> : PageList<T>
    {
        /// <summary>
        /// 耗时单位：S
        /// </summary>
        public string WatchTime { get; set; }
    }
}
