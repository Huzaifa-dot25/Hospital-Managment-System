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
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Payment?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(p => p.Invoice)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IReadOnlyList<Payment>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            return await _dbSet
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PaymentDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedAsync(
            PaymentQueryParams queryParams)
        {
            var query = _dbSet
                .Include(p => p.Invoice)
                .AsQueryable();

            if (queryParams.InvoiceId.HasValue)
                query = query.Where(p => p.InvoiceId == queryParams.InvoiceId.Value);

            if (queryParams.Method.HasValue)
                query = query.Where(p => p.Method == (PaymentMethod)queryParams.Method.Value);

            if (queryParams.Status.HasValue)
                query = query.Where(p => p.Status == (PaymentStatus)queryParams.Status.Value);

            if (queryParams.FromDate.HasValue)
                query = query.Where(p => p.PaymentDate >= queryParams.FromDate.Value);

            if (queryParams.ToDate.HasValue)
                query = query.Where(p => p.PaymentDate <= queryParams.ToDate.Value);

            var totalCount = await query.CountAsync();

            query = queryParams.IsDescending
                ? query.OrderByDescending(p => p.PaymentDate)
                : query.OrderBy(p => p.PaymentDate);

            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
