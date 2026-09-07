using System;

namespace Hospital.Application.DTOs.Pharmacy
{
    public class CreatePrescriptionItemDto
    {
        public Guid MedicationId { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public int DurationInDays { get; set; }
        public int Quantity { get; set; }
        public string Instructions { get; set; } = string.Empty;
    }
}
