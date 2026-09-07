using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    public interface ILabOrderRepository : IRepository<LabOrder>
    {
        Task<LabOrder?> GetByIdWithDetailsAsync(Guid id);
        Task<LabOrder?> GetByIdWithItemsAsync(Guid id);
        Task<(IReadOnlyList<LabOrder> Items, int TotalCount)> GetPagedAsync(LabOrderQueryParams queryParams);
    }
}
