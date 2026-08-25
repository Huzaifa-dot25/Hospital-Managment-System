using System;
using Hospital.Shared.Models;

namespace Hospital.Shared.Queries
{
    /// <summary>
    /// Query parameters for GET /api/v1/doctor.
    ///
    /// Example URLs:
    ///   GET /api/v1/doctor?search=ahmed
    ///   GET /api/v1/doctor?specialization=cardio&amp;departmentId=abc123
    ///   GET /api/v1/doctor?sortBy=yearsOfExperience&amp;isDescending=true
    /// </summary>
    public class DoctorQueryParams : PaginationParams
    {
        /// <summary>
        /// Free-text search across FirstName, LastName, Specialization, LicenseNumber.
        /// </summary>
        public string? Search { get; set; }

        /// <summary>Filter by partial specialization name (e.g. "cardio" matches "Cardiology").</summary>
        public string? Specialization { get; set; }

        /// <summary>Filter doctors belonging to a specific department.</summary>
        public Guid? DepartmentId { get; set; }
    }
}
