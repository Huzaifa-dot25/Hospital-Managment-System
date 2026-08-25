using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    /// <summary>
    /// Doctor-specific repository interface.
    ///
    /// Adds two capabilities on top of the generic IRepository:
    ///   1. Eager-loading (Include Department) — prevents null nav property crashes
    ///   2. Paginated + filtered queries for list endpoints
    /// </summary>
    public interface IDoctorRepository : IRepository<Doctor>
    {
        /// <summary>
        /// Returns all doctors with their Department loaded.
        /// Used internally when no pagination is needed (e.g. dropdown lists).
        /// </summary>
        Task<IReadOnlyList<Doctor>> GetAllWithDepartmentAsync();

        /// <summary>
        /// Returns a single doctor with Department loaded.
        /// </summary>
        Task<Doctor?> GetByIdWithDepartmentAsync(Guid id);

        /// <summary>
        /// Returns a paginated, filtered, sorted page of doctors with Department loaded.
        ///
        /// The tuple (Items, TotalCount) gives us the page data AND total record count
        /// in a single database query — no second round-trip needed.
        /// </summary>
        Task<(IReadOnlyList<Doctor> Items, int TotalCount)> GetPagedAsync(DoctorQueryParams queryParams);
    }
}
