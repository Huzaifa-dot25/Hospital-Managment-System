using System;
using System.Collections.Generic;

namespace Hospital.Application.DTOs.Pharmacy
{
    public class CreatePrescriptionDto
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid? MedicalRecordId { get; set; }
        public DateTime? PrescriptionDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<CreatePrescriptionItemDto> Items { get; set; } = new List<CreatePrescriptionItemDto>();
    }
}
