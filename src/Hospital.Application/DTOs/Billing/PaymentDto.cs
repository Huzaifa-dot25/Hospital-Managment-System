using System;

namespace Hospital.Application.DTOs.Billing
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public Guid? ReceivedByUserId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
