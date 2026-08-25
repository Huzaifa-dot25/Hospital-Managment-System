using Hospital.Shared.Models;

namespace Hospital.Shared.Queries
{
    /// <summary>
    /// Query parameters for the GET /api/v1/patient endpoint.
    /// Lives in Hospital.Shared so both the Domain (repository interfaces)
    /// and Application (service layer) can reference it without circular dependencies.
    /// 
    /// Clean Architecture dependency rule:
    ///   Domain → (nothing)
    ///   Application → Domain
    ///   Shared → (nothing)
    ///   Everyone → Shared ✓
    /// 
    /// Example URLs:
    ///   GET /api/v1/patient?search=john
    ///   GET /api/v1/patient?bloodGroup=1&amp;gender=0&amp;pageSize=5
    ///   GET /api/v1/patient?search=ali&amp;sortBy=lastName&amp;isDescending=true&amp;pageNumber=2
    /// </summary>
    public class PatientQueryParams : PaginationParams
    {
        /// <summary>
        /// Free-text search across FirstName, LastName, ContactNumber.
        /// Case-insensitive SQL LIKE '%value%'.
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Filter by blood group integer value.
        /// 0=APositive, 1=ANegative, 2=BPositive, 3=BNegative,
        /// 4=ABPositive, 5=ABNegative, 6=OPositive, 7=ONegative
        /// </summary>
        public int? BloodGroup { get; set; }

        /// <summary>
        /// Filter by gender integer value.
        /// 0=Male, 1=Female, 2=Other
        /// </summary>
        public int? Gender { get; set; }
    }
}
