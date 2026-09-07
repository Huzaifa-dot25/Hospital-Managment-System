using Hospital.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Hospital.Application.DTOs.Pharmacy
{
    public class PrescriptionDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public Guid? MedicalRecordId { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public PrescriptionStatus Status { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime? DispensedDate { get; set; }
        public Guid? DispensedByPharmacistId { get; set; }
        public List<PrescriptionItemDto> Items { get; set; } = new List<PrescriptionItemDto>();
    }
}
