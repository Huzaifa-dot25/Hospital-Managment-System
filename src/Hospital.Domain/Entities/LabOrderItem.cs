using Hospital.Domain.Common;
using System;

namespace Hospital.Domain.Entities
{
    public class LabOrderItem : BaseEntity
    {
        public Guid LabOrderId { get; set; }
        public LabOrder LabOrder { get; set; } = null!;

        public Guid LabTestId { get; set; }
        public LabTest LabTest { get; set; } = null!;

        public string? ResultValue { get; set; }
        public string? Unit { get; set; }
        public string? ReferenceRange { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Remarks { get; set; }

        public DateTime? PerformedDate { get; set; }
        public Guid? PerformedByLabTechId { get; set; }
    }
}
