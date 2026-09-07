using Hospital.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Hospital.Application.DTOs.Laboratory
{
    public class CreateLabOrderDto
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid? MedicalRecordId { get; set; }
        public LabOrderPriority Priority { get; set; } = LabOrderPriority.Routine;
        public string ClinicalNotes { get; set; } = string.Empty;
        public List<CreateLabOrderItemDto> Items { get; set; } = new();
    }
}
