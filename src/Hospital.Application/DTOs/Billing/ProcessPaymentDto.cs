using Hospital.Domain.Enums;
using System;

namespace Hospital.Application.DTOs.Billing
{
    public class ProcessPaymentDto
    {
        public Guid InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public string TransactionReference { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
