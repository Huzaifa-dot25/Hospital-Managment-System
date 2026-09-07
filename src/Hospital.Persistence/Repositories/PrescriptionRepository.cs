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
    public class PrescriptionRepository : Repository<Prescription>, IPrescriptionRepository
    {
        public PrescriptionRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Prescription?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Medication)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Prescription?> GetByIdWithItemsAsync(Guid id)
        {
            return await _dbSet
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<(IReadOnlyList<Prescription> Items, int TotalCount)> GetPagedAsync(
            PrescriptionQueryParams queryParams)
        {
            var query = _dbSet
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Medication)
                .AsQueryable();

            if (queryParams.PatientId.HasValue)
                query = query.Where(p => p.PatientId == queryParams.PatientId.Value);

            if (queryParams.DoctorId.HasValue)
                query = query.Where(p => p.DoctorId == queryParams.DoctorId.Value);

            if (queryParams.Status.HasValue)
                query = query.Where(p => p.Status == (PrescriptionStatus)queryParams.Status.Value);

            if (queryParams.FromDate.HasValue)
                query = query.Where(p => p.PrescriptionDate >= queryParams.FromDate.Value);

            if (queryParams.ToDate.HasValue)
                query = query.Where(p => p.PrescriptionDate <= queryParams.ToDate.Value);

            var totalCount = await query.CountAsync();

            query = queryParams.IsDescending
                ? query.OrderByDescending(p => p.PrescriptionDate)
                : query.OrderBy(p => p.PrescriptionDate);

            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
