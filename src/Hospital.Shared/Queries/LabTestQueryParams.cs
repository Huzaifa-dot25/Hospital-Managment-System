using Hospital.Shared.Models;

namespace Hospital.Shared.Queries
{
    public class LabTestQueryParams : PaginationParams
    {
        public string? Category { get; set; }
        public string? SampleType { get; set; }
        public string? Search { get; set; }
    }
}
