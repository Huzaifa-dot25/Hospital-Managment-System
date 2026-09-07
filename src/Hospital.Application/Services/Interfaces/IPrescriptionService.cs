using Hospital.Application.DTOs.Pharmacy;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface IPrescriptionService
    {
        Task<PagedResponse<PrescriptionDto>> GetPagedAsync(PrescriptionQueryParams queryParams);
        Task<PrescriptionDto> GetPrescriptionByIdAsync(Guid id);
        Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionDto createDto);
        Task<PrescriptionDto> DispensePrescriptionAsync(Guid id, DispensePrescriptionDto dispenseDto);
        Task CancelPrescriptionAsync(Guid id);
    }
}
