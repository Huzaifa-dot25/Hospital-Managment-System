using Hospital.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    /// <summary>
    /// Doctor-specific repository interface.
    /// Extends the generic IRepository with queries that need related data (eager loading).
    /// </summary>
    public interface IDoctorRepository : IRepository<Doctor>
    {
        /// <summary>
        /// Returns all doctors WITH their Department loaded.
        /// Use this instead of GetAllAsync() when you need the department name in the response.
        /// </summary>
        Task<IReadOnlyList<Doctor>> GetAllWithDepartmentAsync();

        /// <summary>
        /// Returns a single doctor WITH their Department loaded by id.
        /// </summary>
        Task<Doctor?> GetByIdWithDepartmentAsync(Guid id);
    }
}
