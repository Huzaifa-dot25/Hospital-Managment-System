using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Common;
using Hospital.Domain.Entities;
using Hospital.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Hospital.Persistence.Contexts
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        private readonly ICurrentUserService _currentUserService;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentUserService currentUserService) : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<MedicalRecord> MedicalRecords { get; set; } = null!;
        public DbSet<Medication> Medications { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;
        public DbSet<LabTest> LabTests { get; set; } = null!;
        public DbSet<LabOrder> LabOrders { get; set; } = null!;
        public DbSet<LabOrderItem> LabOrderItems { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Crucial for Identity models

            // This will automatically apply all configurations (like PatientConfiguration) 
            // defined in this assembly.
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            
            // Global query filter to automatically filter out soft-deleted entities
            modelBuilder.Entity<Patient>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Doctor>().HasQueryFilter(d => !d.IsDeleted);
            modelBuilder.Entity<Department>().HasQueryFilter(d => !d.IsDeleted);
            modelBuilder.Entity<Appointment>().HasQueryFilter(a => !a.IsDeleted);
            modelBuilder.Entity<MedicalRecord>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Medication>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Prescription>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<PrescriptionItem>().HasQueryFilter(pi => !pi.IsDeleted);
            modelBuilder.Entity<LabTest>().HasQueryFilter(lt => !lt.IsDeleted);
            modelBuilder.Entity<LabOrder>().HasQueryFilter(lo => !lo.IsDeleted);
            modelBuilder.Entity<LabOrderItem>().HasQueryFilter(loi => !loi.IsDeleted);
            modelBuilder.Entity<Invoice>().HasQueryFilter(i => !i.IsDeleted);
            modelBuilder.Entity<InvoiceItem>().HasQueryFilter(ii => !ii.IsDeleted);
            modelBuilder.Entity<Payment>().HasQueryFilter(py => !py.IsDeleted);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.UtcNow;
                        entry.Entity.CreatedBy = _currentUserService.UserId; 
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = _currentUserService.UserId;
                        break;
                    case EntityState.Deleted:
                        // Implement soft delete
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedDate = DateTime.UtcNow;
                        entry.Entity.DeletedBy = _currentUserService.UserId;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
