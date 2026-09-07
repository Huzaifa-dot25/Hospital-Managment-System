using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
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
    public class LabOrderRepository : Repository<LabOrder>, ILabOrderRepository
    {
        public LabOrderRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<LabOrder?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Include(o => o.Items)
                    .ThenInclude(i => i.LabTest)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<LabOrder?> GetByIdWithItemsAsync(Guid id)
        {
            return await _dbSet
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<(IReadOnlyList<LabOrder> Items, int TotalCount)> GetPagedAsync(
            LabOrderQueryParams queryParams)
        {
            var query = _dbSet
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Include(o => o.Items)
                    .ThenInclude(i => i.LabTest)
                .AsQueryable();

            if (queryParams.PatientId.HasValue)
                query = query.Where(o => o.PatientId == queryParams.PatientId.Value);

            if (queryParams.DoctorId.HasValue)
                query = query.Where(o => o.DoctorId == queryParams.DoctorId.Value);

            if (queryParams.Status.HasValue)
                query = query.Where(o => o.Status == (LabOrderStatus)queryParams.Status.Value);

            if (queryParams.Priority.HasValue)
                query = query.Where(o => o.Priority == (LabOrderPriority)queryParams.Priority.Value);

            if (queryParams.FromDate.HasValue)
                query = query.Where(o => o.OrderDate >= queryParams.FromDate.Value);

            if (queryParams.ToDate.HasValue)
                query = query.Where(o => o.OrderDate <= queryParams.ToDate.Value);

            var totalCount = await query.CountAsync();

            query = queryParams.IsDescending
                ? query.OrderByDescending(o => o.OrderDate)
                : query.OrderBy(o => o.OrderDate);

            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
