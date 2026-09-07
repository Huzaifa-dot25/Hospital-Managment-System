using System;
using System.Collections.Generic;

namespace Hospital.Application.DTOs.Billing
{
    public class CreateInvoiceDto
    {
        public Guid PatientId { get; set; }
        public Guid? AppointmentId { get; set; }
        public Guid? PrescriptionId { get; set; }
        public Guid? LabOrderId { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<CreateInvoiceItemDto> Items { get; set; } = new();
    }
}
