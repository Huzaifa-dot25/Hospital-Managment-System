using Hospital.Domain.Entities;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Domain.Repositories
{
    public interface IMedicalRecordRepository : IRepository<MedicalRecord>
    {
        Task<MedicalRecord?> GetByIdWithDetailsAsync(Guid id);
        Task<(IReadOnlyList<MedicalRecord> Items, int TotalCount)> GetPagedAsync(MedicalRecordQueryParams queryParams);
    }
}
