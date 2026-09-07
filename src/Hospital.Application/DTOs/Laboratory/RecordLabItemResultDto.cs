using System;

namespace Hospital.Application.DTOs.Laboratory
{
    public class RecordLabItemResultDto
    {
        public Guid LabItemId { get; set; }
        public string ResultValue { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string? ReferenceRange { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Remarks { get; set; }
    }
}
