using Hospital.Application.DTOs.Laboratory;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface ILabOrderService
    {
        Task<PagedResponse<LabOrderDto>> GetPagedAsync(LabOrderQueryParams queryParams);
        Task<LabOrderDto> GetByIdAsync(Guid id);
        Task<LabOrderDto> CreateOrderAsync(CreateLabOrderDto createDto);
        Task<LabOrderDto> CollectSampleAsync(Guid id, CollectSampleDto collectDto);
        Task<LabOrderDto> RecordResultsAsync(Guid id, RecordLabResultsDto resultsDto, Guid? labTechUserId = null);
        Task<LabOrderDto> CancelOrderAsync(Guid id);
    }
}
