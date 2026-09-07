using Hospital.Shared.Models;
using System;

namespace Hospital.Shared.Queries
{
    public class InvoiceQueryParams : PaginationParams
    {
        public Guid? PatientId { get; set; }
        public int? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
    }
}
