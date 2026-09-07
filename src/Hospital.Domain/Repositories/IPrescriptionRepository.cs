using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    public interface IPrescriptionRepository : IRepository<Prescription>
    {
        Task<Prescription?> GetByIdWithDetailsAsync(Guid id);
        Task<Prescription?> GetByIdWithItemsAsync(Guid id);
        Task<(IReadOnlyList<Prescription> Items, int TotalCount)> GetPagedAsync(PrescriptionQueryParams queryParams);
    }
}
