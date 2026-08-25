using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Persistence.Contexts;
using Hospital.Shared.Queries;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Persistence.Repositories
{
    /// <summary>
    /// Concrete EF Core implementation of IDoctorRepository.
    ///
    /// Every method that returns DoctorDto-mapped data must Include(d => d.Department)
    /// because DoctorProfile maps DepartmentName from the navigation property.
    /// Forgetting Include() causes a NullReferenceException at mapping time.
    /// </summary>
    public class DoctorRepository : Repository<Doctor>, IDoctorRepository
    {
        public DoctorRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Doctor>> GetAllWithDepartmentAsync()
        {
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

        /// <inheritdoc />
        public async Task<(IReadOnlyList<Doctor> Items, int TotalCount)> GetPagedAsync(
            DoctorQueryParams queryParams)
        {
            // Always include Department — the DTO mapping requires it
            var query = _dbSet.Include(d => d.Department).AsQueryable();

            // ── FILTERING ─────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search.ToLower();
                query = query.Where(d =>
                    d.FirstName.ToLower().Contains(search) ||
                    d.LastName.ToLower().Contains(search) ||
                    d.Specialization.ToLower().Contains(search) ||
                    d.LicenseNumber.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(queryParams.Specialization))
            {
                var spec = queryParams.Specialization.ToLower();
                query = query.Where(d => d.Specialization.ToLower().Contains(spec));
            }

            if (queryParams.DepartmentId.HasValue)
            {
                query = query.Where(d => d.DepartmentId == queryParams.DepartmentId.Value);
            }

            // ── SORTING ───────────────────────────────────────────────
            query = queryParams.SortBy?.ToLower() switch
            {
                "firstname"         => queryParams.IsDescending ? query.OrderByDescending(d => d.FirstName)         : query.OrderBy(d => d.FirstName),
                "lastname"          => queryParams.IsDescending ? query.OrderByDescending(d => d.LastName)          : query.OrderBy(d => d.LastName),
                "specialization"    => queryParams.IsDescending ? query.OrderByDescending(d => d.Specialization)    : query.OrderBy(d => d.Specialization),
                "yearsofexperience" => queryParams.IsDescending ? query.OrderByDescending(d => d.YearsOfExperience) : query.OrderBy(d => d.YearsOfExperience),
                "department"        => queryParams.IsDescending ? query.OrderByDescending(d => d.Department.Name)   : query.OrderBy(d => d.Department.Name),
                _                   => query.OrderBy(d => d.LastName) // default: alphabetical by last name
            };

            // ── COUNT + PAGINATE ───────────────────────────────────────
            var totalCount = await query.CountAsync();

            var items = await query
                .AsNoTracking()
                .Skip(queryParams.Skip)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
