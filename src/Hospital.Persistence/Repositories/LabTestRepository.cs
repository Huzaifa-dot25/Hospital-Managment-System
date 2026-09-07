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
    public class LabTestRepository : Repository<LabTest>, ILabTestRepository
    {
        public LabTestRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<(IReadOnlyList<LabTest> Items, int TotalCount)> GetPagedAsync(
            LabTestQueryParams queryParams)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryParams.Category))
                query = query.Where(t => t.Category.ToLower() == queryParams.Category.Trim().ToLower());

            if (!string.IsNullOrWhiteSpace(queryParams.SampleType))
                query = query.Where(t => t.SampleType.ToLower() == queryParams.SampleType.Trim().ToLower());

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var term = queryParams.Search.Trim().ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(term) ||
                    t.Code.ToLower().Contains(term) ||
                    t.Category.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            query = queryParams.SortBy?.ToLower() switch
            {
                "code" => queryParams.IsDescending
                    ? query.OrderByDescending(t => t.Code)
                    : query.OrderBy(t => t.Code),
                "price" => queryParams.IsDescending
                    ? query.OrderByDescending(t => t.Price)
                    : query.OrderBy(t => t.Price),
                "category" => queryParams.IsDescending
                    ? query.OrderByDescending(t => t.Category)
                    : query.OrderBy(t => t.Category),
                _ => queryParams.IsDescending
                    ? query.OrderByDescending(t => t.Name)
                    : query.OrderBy(t => t.Name)
            };

            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
        {
            var query = _dbSet.Where(t => t.Code.ToLower() == code.Trim().ToLower());
            if (excludeId.HasValue)
                query = query.Where(t => t.Id != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}
