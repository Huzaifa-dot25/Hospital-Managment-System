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
    public class MedicalRecordRepository : Repository<MedicalRecord>, IMedicalRecordRepository
    {
        public MedicalRecordRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<MedicalRecord?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.Appointment)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<(IReadOnlyList<MedicalRecord> Items, int TotalCount)> GetPagedAsync(
            MedicalRecordQueryParams queryParams)
        {
            var query = _dbSet
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.Appointment)
                .AsQueryable();

            if (queryParams.PatientId.HasValue)
                query = query.Where(m => m.PatientId == queryParams.PatientId.Value);

            if (queryParams.DoctorId.HasValue)
                query = query.Where(m => m.DoctorId == queryParams.DoctorId.Value);

            if (queryParams.FromDate.HasValue)
                query = query.Where(m => m.RecordDate >= queryParams.FromDate.Value);

            if (queryParams.ToDate.HasValue)
                query = query.Where(m => m.RecordDate <= queryParams.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var term = queryParams.Search.Trim().ToLower();
                query = query.Where(m =>
                    m.Diagnosis.ToLower().Contains(term) ||
                    m.Symptoms.ToLower().Contains(term) ||
                    m.Treatment.ToLower().Contains(term) ||
                    m.Notes.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            query = queryParams.SortBy?.ToLower() switch
            {
                "diagnosis" => queryParams.IsDescending
                    ? query.OrderByDescending(m => m.Diagnosis)
                    : query.OrderBy(m => m.Diagnosis),
                _ => queryParams.IsDescending
                    ? query.OrderByDescending(m => m.RecordDate)
                    : query.OrderBy(m => m.RecordDate)
            };

            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
