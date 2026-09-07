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
    public class MedicationRepository : Repository<Medication>, IMedicationRepository
    {
        public MedicationRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<(IReadOnlyList<Medication> Items, int TotalCount)> GetPagedAsync(
            MedicationQueryParams queryParams)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryParams.Category))
                query = query.Where(m => m.Category.ToLower() == queryParams.Category.Trim().ToLower());

            if (!string.IsNullOrWhiteSpace(queryParams.DosageForm))
                query = query.Where(m => m.DosageForm.ToLower() == queryParams.DosageForm.Trim().ToLower());

            if (queryParams.InStockOnly.HasValue && queryParams.InStockOnly.Value)
                query = query.Where(m => m.StockQuantity > 0);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var term = queryParams.Search.Trim().ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(term) ||
                    m.GenericName.ToLower().Contains(term) ||
                    m.Manufacturer.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            query = queryParams.SortBy?.ToLower() switch
            {
                "price" => queryParams.IsDescending
                    ? query.OrderByDescending(m => m.Price)
                    : query.OrderBy(m => m.Price),
                "stockquantity" => queryParams.IsDescending
                    ? query.OrderByDescending(m => m.StockQuantity)
                    : query.OrderBy(m => m.StockQuantity),
                _ => queryParams.IsDescending
                    ? query.OrderByDescending(m => m.Name)
                    : query.OrderBy(m => m.Name)
            };

            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IReadOnlyList<Medication>> GetLowStockAsync(int threshold)
        {
            return await _dbSet
                .Where(m => m.StockQuantity <= threshold)
                .OrderBy(m => m.StockQuantity)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
