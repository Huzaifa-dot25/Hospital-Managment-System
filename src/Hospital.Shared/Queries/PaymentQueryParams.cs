using Hospital.Shared.Models;
using System;

namespace Hospital.Shared.Queries
{
    public class PaymentQueryParams : PaginationParams
    {
        public Guid? InvoiceId { get; set; }
        public int? Method { get; set; }
        public int? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
