using Hospital.Domain.Repositories;
using Hospital.Persistence.Contexts;
using Hospital.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.Persistence
{
    /// <summary>
    /// Registers all Persistence-layer services (DbContext + Repositories + UnitOfWork).
    /// 
    /// NOTE: ASP.NET Identity registration (.AddIdentity + .AddEntityFrameworkStores)
    /// is done in InfrastructureServiceRegistration because it requires knowledge of 
    /// both the Identity configuration (Infrastructure) AND the DbContext (Persistence).
    /// The API project references both layers and calls both registration methods.
    /// </summary>
    public static class PersistenceServiceRegistration
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register the DbContext with SQL Server.
            // The connection string is read from appsettings.json → ConnectionStrings.HospitalConnectionString
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("HospitalConnectionString")));

            // ─────────────────────────────────────────────────────────────
            // REPOSITORY REGISTRATIONS
            // Scoped = one instance per HTTP request.
            // This ensures all repository operations in one request share the SAME
            // DbContext instance → same transaction boundary.
            // ─────────────────────────────────────────────────────────────
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IDoctorRepository, DoctorRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
