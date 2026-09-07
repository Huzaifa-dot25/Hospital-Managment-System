using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Repositories;
using Hospital.Persistence.Contexts;
using Hospital.Shared.Queries;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Persistence.Repositories
{
    public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Invoice?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(i => i.Patient)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetByIdWithPaymentsAsync(Guid id)
        {
            return await _dbSet
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(
            InvoiceQueryParams queryParams)
        {
            var query = _dbSet
                .Include(i => i.Patient)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .AsQueryable();

            if (queryParams.PatientId.HasValue)
                query = query.Where(i => i.PatientId == queryParams.PatientId.Value);

            if (queryParams.Status.HasValue)
                query = query.Where(i => i.Status == (InvoiceStatus)queryParams.Status.Value);

            if (queryParams.FromDate.HasValue)
                query = query.Where(i => i.IssueDate >= queryParams.FromDate.Value);

            if (queryParams.ToDate.HasValue)
                query = query.Where(i => i.IssueDate <= queryParams.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var term = queryParams.Search.Trim().ToLower();
                query = query.Where(i =>
                    i.InvoiceNumber.ToLower().Contains(term) ||
                    (i.Patient != null && (
                        i.Patient.FirstName.ToLower().Contains(term) ||
                        i.Patient.LastName.ToLower().Contains(term))));
            }

            var totalCount = await query.CountAsync();

            query = queryParams.IsDescending
                ? query.OrderByDescending(i => i.IssueDate)
                : query.OrderBy(i => i.IssueDate);

            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber, Guid? excludeId = null)
        {
            var query = _dbSet.Where(i => i.InvoiceNumber.ToLower() == invoiceNumber.Trim().ToLower());
            if (excludeId.HasValue)
                query = query.Where(i => i.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<int> GetCountForCurrentMonthAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(i => i.IssueDate.Year == now.Year && i.IssueDate.Month == now.Month)
                .CountAsync();
        }
    }
}
