using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Persistence.Repositories
{
    /// <summary>
    /// Concrete implementation of IDoctorRepository.
    /// Adds eager-loading queries on top of the generic Repository base.
    /// 
    /// Why do we need Include()?
    /// EF Core uses "lazy loading off by default". When you fetch a Doctor from the DB,
    /// it only runs: SELECT * FROM Doctors WHERE Id = @id
    /// The Department navigation property stays null until you explicitly tell EF to join it.
    /// .Include(d => d.Department) adds: LEFT JOIN Departments ON Doctors.DepartmentId = Departments.Id
    /// </summary>
    public class DoctorRepository : Repository<Doctor>, IDoctorRepository
    {
        public DoctorRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Doctor>> GetAllWithDepartmentAsync()
        {
            // .Include() tells EF Core to JOIN the Departments table in the same SQL query.
            // AsNoTracking() is a performance optimization for read-only queries:
            // EF won't track changes to these entities, saving memory and CPU.
            return await _dbSet
                .Include(d => d.Department)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Doctor?> GetByIdWithDepartmentAsync(Guid id)
        {
            return await _dbSet
                .Include(d => d.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}
