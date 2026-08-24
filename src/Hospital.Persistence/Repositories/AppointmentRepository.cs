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
    /// Concrete implementation of IAppointmentRepository.
    /// Appointments need BOTH Patient and Doctor data loaded (two separate Includes).
    /// 
    /// Think of it like this:
    /// Appointment table has PatientId and DoctorId (foreign keys - just numbers/GUIDs).
    /// To show "John Smith" and "Dr. Alice Brown" in the API response,
    /// we need to JOIN to the Patients and Doctors tables.
    /// That's exactly what .Include() does.
    /// </summary>
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Appointment>> GetAllWithDetailsAsync()
        {
            // Two separate Include() calls = two JOINs in the generated SQL.
            // EF Core is smart enough to combine them into one efficient query.
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
    }
}
