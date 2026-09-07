using System;
using System.Collections.Generic;

namespace Hospital.Application.DTOs.Laboratory
{
    public class LabOrderDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public Guid? MedicalRecordId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string ClinicalNotes { get; set; } = string.Empty;
        public DateTime? SampleCollectionDate { get; set; }
        public string? SampleCollectedBy { get; set; }
        public DateTime? CompletedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<LabOrderItemDto> Items { get; set; } = new();
    }
}
