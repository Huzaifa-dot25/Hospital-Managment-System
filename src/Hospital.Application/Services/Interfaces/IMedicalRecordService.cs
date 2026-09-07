using Hospital.Application.DTOs.MedicalRecord;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface IMedicalRecordService
    {
        Task<PagedResponse<MedicalRecordDto>> GetPagedAsync(MedicalRecordQueryParams queryParams);
        Task<MedicalRecordDto> GetMedicalRecordByIdAsync(Guid id);
        Task<MedicalRecordDto> CreateMedicalRecordAsync(CreateMedicalRecordDto createDto);
        Task UpdateMedicalRecordAsync(UpdateMedicalRecordDto updateDto);
        Task DeleteMedicalRecordAsync(Guid id);
    }
}
