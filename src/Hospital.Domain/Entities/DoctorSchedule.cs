using Hospital.Domain.Common;
using System;

namespace Hospital.Domain.Entities
{
    public class DoctorSchedule : BaseEntity
    {
        public Guid DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;
        
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxPatients { get; set; }
    }
}
