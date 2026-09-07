using Hospital.Application.DTOs.Pharmacy;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface IMedicationService
    {
        Task<PagedResponse<MedicationDto>> GetPagedAsync(MedicationQueryParams queryParams);
        Task<MedicationDto> GetMedicationByIdAsync(Guid id);
        Task<IReadOnlyList<MedicationDto>> GetLowStockAsync(int threshold);
        Task<MedicationDto> CreateMedicationAsync(CreateMedicationDto createDto);
        Task UpdateMedicationAsync(UpdateMedicationDto updateDto);
        Task DeleteMedicationAsync(Guid id);
    }
}
