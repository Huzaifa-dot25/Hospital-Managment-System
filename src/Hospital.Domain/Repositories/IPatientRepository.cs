using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    /// <summary>
    /// Patient-specific repository interface.
    ///
    /// Why does this interface exist when we have IRepository&lt;Patient&gt;?
    /// IRepository&lt;T&gt; covers basic CRUD.
    /// This adds patient-specific queries like paginated search/filter —
    /// things that don't make sense as generic operations.
    /// </summary>
    public interface IPatientRepository : IRepository<Patient>
    {
        /// <summary>
        /// Returns a paginated, filtered, sorted page of patients.
        ///
        /// Returns a tuple (Items, TotalCount) so the service can build
        /// TotalPages without a second database round-trip.
        /// </summary>
        Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(PatientQueryParams queryParams);
    }
}
