using System;
using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs.Appointment
{
    public class UpdateAppointmentDto
    {
        public Guid Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
