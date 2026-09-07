using Hospital.Shared.Models;

namespace Hospital.Shared.Queries
{
    public class MedicationQueryParams : PaginationParams
    {
        public string? Category { get; set; }
        public string? DosageForm { get; set; }
        public bool? InStockOnly { get; set; }
        public string? Search { get; set; }
    }
}
