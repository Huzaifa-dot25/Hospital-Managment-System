using Hospital.Application.DTOs.Billing;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<PagedResponse<InvoiceDto>> GetPagedAsync(InvoiceQueryParams queryParams);
        Task<InvoiceDto> GetByIdAsync(Guid id);
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto);
        Task<InvoiceDto> CancelInvoiceAsync(Guid id);
    }
}
