using System;

namespace Hospital.Application.DTOs.Laboratory
{
    public class LabTestDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int TurnaroundTimeHours { get; set; }
        public string SampleType { get; set; } = string.Empty;
        public string ReferenceRange { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
