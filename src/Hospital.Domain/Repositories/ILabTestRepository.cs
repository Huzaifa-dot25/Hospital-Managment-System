using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    public interface ILabTestRepository : IRepository<LabTest>
    {
        Task<(IReadOnlyList<LabTest> Items, int TotalCount)> GetPagedAsync(LabTestQueryParams queryParams);
        Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null);
    }
}
