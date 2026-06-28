using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SqlBoTx.Net.EFCore
{
    /// <summary>
    /// EF Core IQueryable 查询扩展
    /// </summary>
    public static class EFCoreQueryExtension
    {
        #region WhereIf 条件过滤

        /// <summary>
        /// 当 condition 为 true 时,追加 Where 条件
        /// </summary>
        public static IQueryable<T> WhereIf<T>(
            this IQueryable<T> query,
            bool condition,
            Expression<Func<T, bool>> predicate)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            return condition ? query.Where(predicate) : query;
        }

        /// <summary>
        /// 当字符串非空时,追加 Where 条件(常用于动态搜索)
        /// </summary>
        public static IQueryable<T> WhereIfNotEmpty<T>(
            this IQueryable<T> query,
            string value,
            Expression<Func<T, bool>> predicate)
            => query.WhereIf(!string.IsNullOrWhiteSpace(value), predicate);

        /// <summary>
        /// 当可空值有值时,追加 Where 条件
        /// </summary>
        public static IQueryable<T> WhereIfHasValue<T, TValue>(
            this IQueryable<T> query,
            TValue? value,
            Expression<Func<T, bool>> predicate) where TValue : struct
            => query.WhereIf(value.HasValue, predicate);

        #endregion

        #region 分页查询

        /// <summary>
        /// 同步分页查询
        /// </summary>
        /// <param name="query">查询源(建议已 OrderBy)</param>
        /// <param name="page">页码,从 1 开始;小于 1 时按 1 处理</param>
        /// <param name="limit">每页大小,必须大于 0</param>
        public static PagedResult<T> ToPagedList<T>(
            this IQueryable<T> query,
            int page,
            int limit)
        {
            ValidatePaging(query, ref page, limit);

            var total = query.LongCount();
            var items = total == 0
                ? new List<T>()
                : query.Skip((page - 1) * limit).Take(limit).ToList();

            return BuildResult(items, total, page, limit);
        }

        /// <summary>
        /// 异步分页查询
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedListAsync<T>(
            this IQueryable<T> query,
            int page,
            int limit,
            CancellationToken cancellationToken = default)
        {
            ValidatePaging(query, ref page, limit);

            var total = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);
            var items = total == 0
                ? new List<T>()
                : await query.Skip((page - 1) * limit)
                             .Take(limit)
                             .ToListAsync(cancellationToken)
                             .ConfigureAwait(false);

            return BuildResult(items, total, page, limit);
        }

        /// <summary>
        /// 异步分页查询(带耗时统计,适合调试/慢查询分析)
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedListWithTimingAsync<T>(
            this IQueryable<T> query,
            int page,
            int limit,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var result = await query.ToPagedListAsync(page, limit, cancellationToken)
                                    .ConfigureAwait(false);
            sw.Stop();
            result.ElapsedMs = sw.ElapsedMilliseconds;
            return result;
        }

        /// <summary>
        /// 键集(KeySet / Cursor)分页 —— 适合大数据量、深翻页场景
        /// 调用方需自己拼好 Where 条件(如 x.Id > lastId)并 OrderBy
        /// </summary>
        /// <param name="query">已应用 cursor 过滤和排序的查询</param>
        /// <param name="limit">每页大小</param>
        public static Task<List<T>> ToKeySetPageAsync<T>(
            this IQueryable<T> query,
            int limit,
            CancellationToken cancellationToken = default)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));
            if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit), "limit 必须大于 0");

            return query.Take(limit).ToListAsync(cancellationToken);
        }

        #endregion

        #region 私有辅助方法
        private static void ValidatePaging<T>(IQueryable<T> query, ref int page, int limit)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));
            if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit), "limit 必须大于 0");
            if (page < 1) page = 1;
        }

        private static PagedResult<T> BuildResult<T>(List<T> items, long total, int page, int limit)
        {
            return new PagedResult<T>
            {
                Items = items,
                TotalRow = total,
                TotalPage = (int)Math.Ceiling((decimal)total / limit),
                PageIndex = page,
                PageSize = limit
            };
        }
        #endregion
    }

    /// <summary>
    /// 分页结果
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>当前页数据</summary>
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        /// <summary>总记录数</summary>
        public long TotalRow { get; set; }

        /// <summary>总页数</summary>
        public int TotalPage { get; set; }

        /// <summary>当前页码(从 1 开始)</summary>
        public int PageIndex { get; set; }

        /// <summary>每页大小</summary>
        public int PageSize { get; set; }

        /// <summary>是否有上一页</summary>
        public bool HasPrevious => PageIndex > 1;

        /// <summary>是否有下一页</summary>
        public bool HasNext => PageIndex < TotalPage;

        /// <summary>查询耗时(毫秒),仅 WithTiming 版本会赋值</summary>
        public long? ElapsedMs { get; set; }
    }
}