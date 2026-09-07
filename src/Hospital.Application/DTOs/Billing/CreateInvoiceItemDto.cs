using Hospital.Domain.Enums;
using System;

namespace Hospital.Application.DTOs.Billing
{
    public class CreateInvoiceItemDto
    {
        public BillingItemType ItemType { get; set; } = BillingItemType.Other;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public Guid? ReferenceId { get; set; }
    }
}
