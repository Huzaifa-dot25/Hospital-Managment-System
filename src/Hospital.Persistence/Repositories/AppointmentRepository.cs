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
    /// <summary>
    /// Concrete EF Core implementation of IAppointmentRepository.
    ///
    /// Appointments always require two Includes:
    ///   .Include(a => a.Patient)
    ///   .Include(a => a.Doctor)
    ///
    /// Without these, AppointmentProfile tries to read:
    ///   src.Patient.FirstName  → NullReferenceException
    ///   src.Doctor.LastName    → NullReferenceException
    /// </summary>
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Appointment>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Appointment?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(
            AppointmentQueryParams queryParams)
        {
            // Always include Patient and Doctor — required for DTO mapping
            var query = _dbSet
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            // ── FILTERING ─────────────────────────────────────────────
            if (queryParams.PatientId.HasValue)
                query = query.Where(a => a.PatientId == queryParams.PatientId.Value);

            if (queryParams.DoctorId.HasValue)
                query = query.Where(a => a.DoctorId == queryParams.DoctorId.Value);

            if (queryParams.Status.HasValue)
            {
                // Cast the integer from the query param to the AppointmentStatus enum.
                // This is safe because AppointmentStatus values are 0-3.
                var status = (AppointmentStatus)queryParams.Status.Value;
                query = query.Where(a => a.Status == status);
            }

            // Date range filtering: appointments that fall within [FromDate, ToDate]
            if (queryParams.FromDate.HasValue)
                query = query.Where(a => a.AppointmentDate >= queryParams.FromDate.Value);

            if (queryParams.ToDate.HasValue)
                // Add one day and use < so ToDate is inclusive for the whole day
                query = query.Where(a => a.AppointmentDate < queryParams.ToDate.Value.AddDays(1));

            // ── SORTING ───────────────────────────────────────────────
            query = queryParams.SortBy?.ToLower() switch
            {
                "appointmentdate" => queryParams.IsDescending ? query.OrderByDescending(a => a.AppointmentDate) : query.OrderBy(a => a.AppointmentDate),
                "status"          => queryParams.IsDescending ? query.OrderByDescending(a => a.Status)          : query.OrderBy(a => a.Status),
                "patientname"     => queryParams.IsDescending ? query.OrderByDescending(a => a.Patient.LastName) : query.OrderBy(a => a.Patient.LastName),
                "doctorname"      => queryParams.IsDescending ? query.OrderByDescending(a => a.Doctor.LastName)  : query.OrderBy(a => a.Doctor.LastName),
                _                 => query.OrderByDescending(a => a.AppointmentDate) // default: newest first
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
