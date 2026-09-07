using Hospital.Domain.Common;
using Hospital.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Hospital.Domain.Entities
{
    public class LabOrder : BaseEntity
    {
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public Guid DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

        public Guid? MedicalRecordId { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public LabOrderStatus Status { get; set; } = LabOrderStatus.Ordered;
        public LabOrderPriority Priority { get; set; } = LabOrderPriority.Routine;
        public string ClinicalNotes { get; set; } = string.Empty;

        public DateTime? SampleCollectionDate { get; set; }
        public string? SampleCollectedBy { get; set; }

        public DateTime? CompletedDate { get; set; }

        public ICollection<LabOrderItem> Items { get; set; } = new List<LabOrderItem>();
    }
}
