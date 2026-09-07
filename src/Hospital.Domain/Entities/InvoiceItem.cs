using Hospital.Domain.Common;
using Hospital.Domain.Enums;
using System;

namespace Hospital.Domain.Entities
{
    public class InvoiceItem : BaseEntity
    {
        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public BillingItemType ItemType { get; set; } = BillingItemType.Other;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal TotalPrice { get; set; }
        public Guid? ReferenceId { get; set; }
    }
}
