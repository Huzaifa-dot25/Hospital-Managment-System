using Hospital.Persistence.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // CUSTOMWEBAPPLICATIONFACTORY
    //
    // APPROACH — UseSetting before ConfigureServices:
    //
    //   builder.UseSetting("UseDatabase", "Sqlite") injects the key into
    //   IConfiguration BEFORE Program.cs calls AddPersistenceServices().
    //   PersistenceServiceRegistration reads this key and calls UseSqlite
    //   instead of UseSqlServer. EF Core's internal provider cache is built
    //   with SQLite only — the dual-provider error never occurs.
    //
    //   builder.UseSetting("ConnectionStrings:HospitalConnectionString", ...)
    //   replaces the SQL Server connection string with the SQLite named
    //   in-memory connection string.
    //
    // KEEP-ALIVE CONNECTION:
    //   SQLite in-memory databases are destroyed when all connections close.
    //   _keepAliveConnection is opened in the constructor and closed in Dispose(),
    //   keeping the database alive for the entire test class lifetime.
    //
    // DB INIT — done lazily on first CreateUnauthenticatedClient() call:
    //   this.Services (the real host service provider) is only available after
    //   CreateClient() starts the host. We use it to call EnsureCreated() and
    //   seed the 11 Identity roles exactly once.
    // ─────────────────────────────────────────────────────────────────────────
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _keepAliveConnection;
        private readonly string _connectionString;
        private bool _initialized;

        public CustomWebApplicationFactory()
        {
            _connectionString = $"DataSource=TestDb_{Guid.NewGuid()};Mode=Memory;Cache=Shared";
            _keepAliveConnection = new SqliteConnection(_connectionString);
            _keepAliveConnection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Inject config keys BEFORE ConfigureServices runs.
            // PersistenceServiceRegistration reads these and calls UseSqlite.
            builder.UseSetting("UseDatabase", "Sqlite");
            builder.UseSetting("ConnectionStrings:HospitalConnectionString", _connectionString);

            // Suppress log noise — EF Core query logs, Serilog console output, etc.
            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // CreateUnauthenticatedClient
        //
        // Returns an HttpClient with no Authorization header.
        // On first call, initializes the SQLite schema and seeds roles using
        // this.Services — the real host service provider built by WebApplicationFactory.
        // ─────────────────────────────────────────────────────────────────────
        public HttpClient CreateUnauthenticatedClient()
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            if (!_initialized)
            {
                _initialized = true;
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                SeedRolesAsync(scope.ServiceProvider).GetAwaiter().GetResult();
            }

            return client;
        }

        private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider
                .GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<
                    Microsoft.AspNetCore.Identity.IdentityRole<Guid>>>();

            var roles = new[]
            {
                "SuperAdmin", "Admin", "Doctor", "Receptionist", "Nurse",
                "Pharmacist", "LabTechnician", "Radiologist", "Cashier",
                "Patient", "Accountant"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(
                        new Microsoft.AspNetCore.Identity.IdentityRole<Guid>(role));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _keepAliveConnection.Dispose();
            base.Dispose(disposing);
        }
    }
}
