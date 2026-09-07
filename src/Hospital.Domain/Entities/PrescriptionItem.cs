using Hospital.Domain.Common;
using System;

namespace Hospital.Domain.Entities
{
    public class PrescriptionItem : BaseEntity
    {
        public Guid PrescriptionId { get; set; }
        public Prescription Prescription { get; set; } = null!;

        public Guid MedicationId { get; set; }
        public Medication Medication { get; set; } = null!;

        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public int DurationInDays { get; set; }
        public int Quantity { get; set; }
        public string Instructions { get; set; } = string.Empty;
    }
}
