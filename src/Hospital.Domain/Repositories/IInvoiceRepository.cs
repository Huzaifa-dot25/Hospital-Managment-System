using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        Task<Invoice?> GetByIdWithDetailsAsync(Guid id);
        Task<Invoice?> GetByIdWithPaymentsAsync(Guid id);
        Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(InvoiceQueryParams queryParams);
        Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber, Guid? excludeId = null);
        Task<int> GetCountForCurrentMonthAsync();
    }
}
