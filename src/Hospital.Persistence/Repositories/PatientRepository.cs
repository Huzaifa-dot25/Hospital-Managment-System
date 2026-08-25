using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Persistence.Contexts;
using Hospital.Shared.Queries;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Persistence.Repositories
{
    /// <summary>
    /// Concrete EF Core implementation of IPatientRepository.
    ///
    /// The key method here is GetPagedAsync — it builds a dynamic LINQ query
    /// that adds WHERE clauses only when the filter has a value.
    /// This produces clean, efficient SQL without unnecessary conditions.
    /// </summary>
    public class PatientRepository : Repository<Patient>, IPatientRepository
    {
        public PatientRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(
            PatientQueryParams queryParams)
        {
            // Start with all non-deleted patients.
            // The global query filter in DbContext already applies IsDeleted = false,
            // so we don't need to add that condition manually.
            var query = _dbSet.AsQueryable();

            // ─────────────────────────────────────────────────────────────
            // FILTERING
            // Each filter is only applied when the parameter has a value (not null).
            // This is called a "dynamic query" — it builds up the WHERE clause
            // based on what the caller actually provided.
            //
            // Why .AsNoTracking()? We're only reading, not updating.
            // Skipping change tracking saves memory and CPU on large result sets.
            // ─────────────────────────────────────────────────────────────

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                // .ToLower() makes the search case-insensitive.
                // SQL Server is case-insensitive by default, but this also works
                // on SQLite (useful for tests). EF Core translates this to LOWER() in SQL.
                var search = queryParams.Search.ToLower();
                query = query.Where(p =>
                    p.FirstName.ToLower().Contains(search) ||
                    p.LastName.ToLower().Contains(search) ||
                    p.ContactNumber.Contains(search));
            }

            if (queryParams.BloodGroup.HasValue)
            {
                // The BloodGroup stored in DB is an int (enum stored as int).
                // We cast the filter int to the enum type for the comparison.
                var bloodGroup = (Hospital.Domain.Enums.BloodGroup)queryParams.BloodGroup.Value;
                query = query.Where(p => p.BloodGroup == bloodGroup);
            }

            if (queryParams.Gender.HasValue)
            {
                var gender = (Hospital.Domain.Enums.Gender)queryParams.Gender.Value;
                query = query.Where(p => p.Gender == gender);
            }

            // ─────────────────────────────────────────────────────────────
            // SORTING
            // Apply the requested sort field, or fall back to CreatedDate desc.
            // We use a switch expression to map field name strings to LINQ expressions.
            //
            // Why not just do .OrderBy(queryParams.SortBy)?
            // Because you can't pass a property name as a string to LINQ directly.
            // You'd need dynamic LINQ or reflection. The switch is safer and explicit.
            // ─────────────────────────────────────────────────────────────
            query = queryParams.SortBy?.ToLower() switch
            {
                "firstname"   => queryParams.IsDescending ? query.OrderByDescending(p => p.FirstName)   : query.OrderBy(p => p.FirstName),
                "lastname"    => queryParams.IsDescending ? query.OrderByDescending(p => p.LastName)    : query.OrderBy(p => p.LastName),
                "dateofbirth" => queryParams.IsDescending ? query.OrderByDescending(p => p.DateOfBirth) : query.OrderBy(p => p.DateOfBirth),
                "createdate"  => queryParams.IsDescending ? query.OrderByDescending(p => p.CreatedDate) : query.OrderBy(p => p.CreatedDate),
                _             => query.OrderByDescending(p => p.CreatedDate) // default: newest first
            };

            // ─────────────────────────────────────────────────────────────
            // COUNT — run BEFORE Skip/Take so it counts ALL matching records,
            // not just the current page.
            //
            // EF Core is smart: it uses the same IQueryable (with all your WHERE
            // clauses already applied) but generates a separate COUNT(*) SQL query.
            // ─────────────────────────────────────────────────────────────
            var totalCount = await query.CountAsync();

            // ─────────────────────────────────────────────────────────────
            // PAGINATION — Skip (OFFSET) and Take (FETCH NEXT) for the current page.
            //
            // SQL generated (roughly):
            //   SELECT * FROM Patients
            //   WHERE IsDeleted = 0 AND FirstName LIKE '%ali%'
            //   ORDER BY CreatedDate DESC
            //   OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY
            // ─────────────────────────────────────────────────────────────
            var items = await query
                .AsNoTracking()
                .Skip(queryParams.Skip)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
