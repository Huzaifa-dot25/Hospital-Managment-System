using System;

namespace Hospital.Application.DTOs.Laboratory
{
    public class LabOrderItemDto
    {
        public Guid Id { get; set; }
        public Guid LabTestId { get; set; }
        public string LabTestName { get; set; } = string.Empty;
        public string LabTestCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ResultValue { get; set; }
        public string? Unit { get; set; }
        public string? ReferenceRange { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Remarks { get; set; }
        public DateTime? PerformedDate { get; set; }
        public Guid? PerformedByLabTechId { get; set; }
    }
}
