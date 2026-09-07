using System;
using System.Collections.Generic;

namespace Hospital.Application.DTOs.Billing
{
    public class InvoiceDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public Guid? AppointmentId { get; set; }
        public Guid? PrescriptionId { get; set; }
        public Guid? LabOrderId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceDue => TotalAmount - PaidAmount;
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<InvoiceItemDto> Items { get; set; } = new();
        public List<PaymentDto> Payments { get; set; } = new();
    }
}
