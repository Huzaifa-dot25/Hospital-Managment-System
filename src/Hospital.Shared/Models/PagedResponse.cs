using System;
using System.Collections.Generic;

namespace Hospital.Shared.Models
{
    /// <summary>
    /// Wraps a paginated list of items with metadata about the pagination.
    /// 
    /// When a frontend receives this, it knows:
    ///   - The actual data (Items)
    ///   - Which page this is (PageNumber)
    ///   - How big each page is (PageSize)
    ///   - Total matching records in the DB (TotalCount)
    ///   - Total pages available (TotalPages) — so it can render a page selector
    ///   - Whether a previous/next page exists (HasPreviousPage/HasNextPage)
    /// 
    /// Example JSON response:
    /// {
    ///   "items": [...],
    ///   "pageNumber": 2,
    ///   "pageSize": 10,
    ///   "totalCount": 847,
    ///   "totalPages": 85,
    ///   "hasPreviousPage": true,
    ///   "hasNextPage": true
    /// }
    /// </summary>
    public class PagedResponse<T>
    {
        /// <summary>The list of items for the current page.</summary>
        public IReadOnlyList<T> Items { get; set; } = new List<T>();

        /// <summary>Current page number (1-based).</summary>
        public int PageNumber { get; set; }

        /// <summary>Number of items per page.</summary>
        public int PageSize { get; set; }

        /// <summary>Total number of records matching the query (across ALL pages).</summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages.
        /// Math.Ceiling ensures we always round up:
        ///   847 records ÷ 10 per page = 84.7 → rounds up to 85 pages.
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>True if there is a page before this one.</summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>True if there is a page after this one.</summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Factory method — creates a PagedResponse from a list + pagination metadata.
        /// Usage: PagedResponse&lt;PatientDto&gt;.Create(dtos, totalCount, queryParams)
        /// </summary>
        public static PagedResponse<T> Create(
            IReadOnlyList<T> items,
            int totalCount,
            int pageNumber,
            int pageSize)
        {
            return new PagedResponse<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
