using Hospital.Application.DTOs.Billing;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PagedResponse<PaymentDto>> GetPagedAsync(PaymentQueryParams queryParams);
        Task<PaymentDto> GetByIdAsync(Guid id);
        Task<IReadOnlyList<PaymentDto>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto paymentDto, Guid? cashierUserId = null);
    }
}
