using System;

namespace Hospital.Application.DTOs.MedicalRecord
{
    public class UpdateMedicalRecordDto
    {
        public Guid Id { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string Symptoms { get; set; } = string.Empty;
        public string Treatment { get; set; } = string.Empty;
        public string Prescription { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
