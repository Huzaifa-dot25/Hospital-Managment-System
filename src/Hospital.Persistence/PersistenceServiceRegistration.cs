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
    /// </summary>
    public static class PersistenceServiceRegistration
    {
        public static IServiceCollection AddPersistenceServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ─────────────────────────────────────────────────────────────────
            // DBCONTEXT REGISTRATION
            //
            // EF Core 9/10 caches the database provider internally per DbContext
            // type. If two providers (SqlServer + Sqlite) are ever registered for
            // the same DbContext — even via ConfigureTestServices overrides —
            // EF Core throws at runtime.
            //
            // Solution: read a "UseDatabase" configuration key injected by the
            // integration test factory via builder.UseSetting() BEFORE services
            // are configured. This ensures ONLY ONE provider is registered from
            // the very first AddDbContext call.
            //
            //   Production:         UseDatabase not set → "SqlServer" → UseSqlServer
            //   Integration tests:  UseDatabase = "Sqlite" → UseSqlite
            // ─────────────────────────────────────────────────────────────────
            var connectionString = configuration.GetConnectionString("HospitalConnectionString")!;
            var useDatabase = configuration["UseDatabase"] ?? "SqlServer";

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                if (useDatabase.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                    options.UseSqlite(connectionString);
                else
                    options.UseSqlServer(connectionString);
            });

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
