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
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
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
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.UtcNow;
                        // To do: Get user id from current user context
                        entry.Entity.CreatedBy = "System"; 
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = "System";
                        break;
                    case EntityState.Deleted:
                        // Implement soft delete
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedDate = DateTime.UtcNow;
                        entry.Entity.DeletedBy = "System";
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
