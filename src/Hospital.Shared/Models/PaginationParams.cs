namespace Hospital.Shared.Models
{
    /// <summary>
    /// Base class for all query/filter request objects.
    /// Every "get list" endpoint should accept these parameters.
    /// 
    /// Why a base class instead of just adding properties everywhere?
    /// Because ALL list endpoints share pagination and sorting.
    /// A single base class means one place to change the max page size,
    /// one place to add a new sorting option, etc. That's the DRY principle.
    /// 
    /// Usage example — a controller action:
    ///   [HttpGet]
    ///   public async Task<IActionResult> GetPatients([FromQuery] PatientQueryParams query)
    ///   
    /// The [FromQuery] attribute tells ASP.NET Core to read values from the URL:
    ///   GET /api/v1/patient?pageNumber=2&amp;pageSize=10&amp;sortBy=lastName&amp;isDescending=true
    /// </summary>
    public class PaginationParams
    {
        /// <summary>
        /// The maximum number of records allowed per page.
        /// This is a server-side safety guard — a client can't ask for 100,000 records.
        /// </summary>
        private const int MaxPageSize = 50;

        private int _pageSize = 10;

        /// <summary>
        /// Which page of results to return. Starts at 1 (not 0).
        /// Default is 1 (first page).
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// How many records per page (1–50, default 10).
        /// The setter enforces the MaxPageSize cap automatically.
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 1 : value;
        }

        /// <summary>
        /// Field name to sort by (e.g. "firstName", "createdDate").
        /// Case-insensitive. If null or empty, the default sort order is used.
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// If true, sort descending (Z→A, newest first).
        /// If false (default), sort ascending (A→Z, oldest first).
        /// </summary>
        public bool IsDescending { get; set; } = false;

        /// <summary>
        /// Calculates how many records to skip in SQL.
        /// SQL OFFSET = (PageNumber - 1) * PageSize
        /// Example: Page 3, Size 10 → skip 20 records → return records 21–30.
        /// </summary>
        public int Skip => (PageNumber - 1) * PageSize;
    }
}
