using System;

namespace Hospital.Application.DTOs.Doctor
{
    public class DoctorScheduleDto
    {
        public Guid Id { get; set; }
        public Guid DoctorId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxPatients { get; set; }
    }
}
