using Hospital.Domain.Common;
using Hospital.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Hospital.Domain.Entities
{
    public class Prescription : BaseEntity
    {
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public Guid DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

        public Guid? MedicalRecordId { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }

        public DateTime PrescriptionDate { get; set; } = DateTime.UtcNow;
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Pending;
        public string Notes { get; set; } = string.Empty;

        public DateTime? DispensedDate { get; set; }
        public Guid? DispensedByPharmacistId { get; set; }

        public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    }
}
