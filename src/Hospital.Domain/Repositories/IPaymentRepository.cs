using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<Payment?> GetByIdWithDetailsAsync(Guid id);
        Task<IReadOnlyList<Payment>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedAsync(PaymentQueryParams queryParams);
    }
}
