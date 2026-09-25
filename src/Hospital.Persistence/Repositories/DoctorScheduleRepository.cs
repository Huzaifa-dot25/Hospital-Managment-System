using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Persistence.Repositories
{
    public class DoctorScheduleRepository : Repository<DoctorSchedule>, IDoctorScheduleRepository
    {
        public DoctorScheduleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<DoctorSchedule>> GetSchedulesByDoctorIdAsync(Guid doctorId)
        {
            return await _dbSet
                .Where(s => s.DoctorId == doctorId)
                .ToListAsync();
        }
    }
}
