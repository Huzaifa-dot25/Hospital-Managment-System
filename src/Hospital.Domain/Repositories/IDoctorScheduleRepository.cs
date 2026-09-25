using Hospital.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    public interface IDoctorScheduleRepository : IRepository<DoctorSchedule>
    {
        Task<IEnumerable<DoctorSchedule>> GetSchedulesByDoctorIdAsync(System.Guid doctorId);
    }
}
