using Hospital.Domain.Common;
using System;

namespace Hospital.Domain.Entities
{
    public class MedicalRecord : BaseEntity
    {
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public Guid DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

        public Guid? AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }

        public DateTime RecordDate { get; set; } = DateTime.UtcNow;
        public string Diagnosis { get; set; } = string.Empty;
        public string Symptoms { get; set; } = string.Empty;
        public string Treatment { get; set; } = string.Empty;
        public string Prescription { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
