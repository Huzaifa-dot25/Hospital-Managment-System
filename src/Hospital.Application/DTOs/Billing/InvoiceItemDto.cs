using System;

namespace Hospital.Application.DTOs.Billing
{
    public class InvoiceItemDto
    {
        public Guid Id { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public Guid? ReferenceId { get; set; }
    }
}
