using Hospital.Domain.Common;
using Hospital.Domain.Enums;
using System;

namespace Hospital.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public PaymentStatus Status { get; set; } = PaymentStatus.Success;
        public string TransactionReference { get; set; } = string.Empty;
        public Guid? ReceivedByUserId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
