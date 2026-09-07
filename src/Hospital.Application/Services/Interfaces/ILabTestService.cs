using Hospital.Application.DTOs.Laboratory;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface ILabTestService
    {
        Task<PagedResponse<LabTestDto>> GetPagedAsync(LabTestQueryParams queryParams);
        Task<LabTestDto> GetByIdAsync(Guid id);
        Task<LabTestDto> CreateAsync(CreateLabTestDto createDto);
        Task UpdateAsync(UpdateLabTestDto updateDto);
        Task DeleteAsync(Guid id);
    }
}
