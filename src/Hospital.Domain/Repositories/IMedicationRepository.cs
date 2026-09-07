using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    public interface IMedicationRepository : IRepository<Medication>
    {
        Task<(IReadOnlyList<Medication> Items, int TotalCount)> GetPagedAsync(MedicationQueryParams queryParams);
        Task<IReadOnlyList<Medication>> GetLowStockAsync(int threshold);
    }
}
