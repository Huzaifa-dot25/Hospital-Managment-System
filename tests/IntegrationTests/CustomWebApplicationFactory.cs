using Hospital.Domain.Entities.Identity;
using Hospital.Persistence.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // CUSTOMWEBAPPLICATIONFACTORY
    //
    // - Uses builder.UseSetting to inject "UseDatabase=Sqlite" and the SQLite
    //   connection string BEFORE AddPersistenceServices runs, so EF Core's
    //   internal provider cache is built with SQLite only (EF Core 9/10 fix).
    //
    // - Keeps one SqliteConnection open for the factory lifetime so the
    //   in-memory DB survives across all requests in the test run.
    //
    // - On first CreateUnauthenticatedClient(), seeds roles AND admin users
    //   directly into the database. Admin/SuperAdmin cannot self-register
    //   (blocked by RegisterDtoValidator for security), so we create them
    //   via UserManager/RoleManager — exactly as production would seed them.
    // ─────────────────────────────────────────────────────────────────────────
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _keepAliveConnection;
        private readonly string _connectionString;
        private bool _initialized;

        // Pre-seeded admin credentials — tests use these to log in as Admin/SuperAdmin
        public const string AdminEmail      = "admin@hospital.test";
        public const string SuperAdminEmail = "superadmin@hospital.test";
        public const string SeededPassword  = "Admin@Password1!";

        public CustomWebApplicationFactory()
        {
            _connectionString = $"DataSource=TestDb_{Guid.NewGuid()};Mode=Memory;Cache=Shared";
            _keepAliveConnection = new SqliteConnection(_connectionString);
            _keepAliveConnection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Inject BEFORE ConfigureServices so PersistenceServiceRegistration
            // calls UseSqlite instead of UseSqlServer — single provider only.
            builder.UseSetting("UseDatabase", "Sqlite");
            builder.UseSetting("ConnectionStrings:HospitalConnectionString", _connectionString);

            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });
        }

        public HttpClient CreateUnauthenticatedClient()
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Init once after host is fully built (Services is available post-CreateClient)
            if (!_initialized)
            {
                _initialized = true;
                using var scope = Services.CreateScope();
                var sp = scope.ServiceProvider;

                var db = sp.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();

                SeedAsync(sp).GetAwaiter().GetResult();
            }

            return client;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SeedAsync — seeds roles AND privileged users
        //
        // Roles: all 11 hospital roles in AspNetRoles
        // Users: one Admin and one SuperAdmin created via UserManager so tests
        //        can log in with those roles without hitting the self-registration
        //        validator (which correctly blocks Admin/SuperAdmin self-signup).
        // ─────────────────────────────────────────────────────────────────────
        private static async Task SeedAsync(IServiceProvider sp)
        {
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            var roles = new[]
            {
                "SuperAdmin", "Admin", "Doctor", "Receptionist", "Nurse",
                "Pharmacist", "LabTechnician", "Radiologist", "Cashier",
                "Patient", "Accountant"
            };

            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));

            // Seed Admin user
            await CreateUserWithRoleAsync(userManager, AdminEmail, SeededPassword, "Admin");

            // Seed SuperAdmin user
            await CreateUserWithRoleAsync(userManager, SuperAdminEmail, SeededPassword, "SuperAdmin");
        }

        private static async Task CreateUserWithRoleAsync(
            UserManager<ApplicationUser> userManager,
            string email, string password, string role)
        {
            if (await userManager.FindByEmailAsync(email) != null)
                return; // already seeded

            var user = new ApplicationUser
            {
                Id        = Guid.NewGuid(),
                UserName  = email,
                Email     = email,
                FirstName = role,
                LastName  = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _keepAliveConnection.Dispose();
            base.Dispose(disposing);
        }
    }
}
