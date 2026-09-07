using Hospital.Domain.Common;
using Hospital.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Hospital.Domain.Entities
{
    public class Invoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public Guid? AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }

        public Guid? PrescriptionId { get; set; }
        public Prescription? Prescription { get; set; }

        public Guid? LabOrderId { get; set; }
        public LabOrder? LabOrder { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public decimal SubTotal { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
        public string Notes { get; set; } = string.Empty;

        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
