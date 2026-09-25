using System;

namespace Hospital.Application.DTOs.Doctor
{
    public class CreateDoctorScheduleDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxPatients { get; set; }
    }
}
