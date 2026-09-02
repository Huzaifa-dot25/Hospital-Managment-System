using Hospital.Persistence;
using Hospital.Application;
using Hospital.Infrastructure;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// SERILOG CONFIGURATION
// Serilog is a structured logging library. "Structured" means log data is stored
// as key-value pairs, not just plain text. This makes logs searchable and filterable.
// ReadFrom.Configuration reads the Serilog settings from appsettings.json.
// ─────────────────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// ─────────────────────────────────────────────────────────────────────────────
// REGISTER SERVICES (Dependency Injection Container)
// Think of this as a registry. We're saying:
// "When anyone asks for IDepartmentService, give them DepartmentService"
// ASP.NET Core handles the creation and lifetime of these objects automatically.
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as their string name instead of their integer value.
        //
        // Without this:  "gender": 0,  "status": 1,  "bloodGroup": 2
        // With this:     "gender": "Male",  "status": "Completed",  "bloodGroup": "BPositive"
        //
        // This makes the API self-documenting — clients don't need a separate
        // enum lookup table. It applies to both serialization (response)
        // and deserialization (request body), so "Male" and 0 both work as input.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());

        // Use camelCase for all JSON property names (standard for REST APIs).
        // "FirstName" in C# → "firstName" in JSON.
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();

// ─────────────────────────────────────────────────────────────────────────────
// SWAGGER WITH JWT SUPPORT
// Swagger is a UI tool that auto-generates interactive API documentation.
// AddSecurityDefinition tells Swagger: "This API uses JWT Bearer tokens"
// AddSecurityRequirement tells Swagger: "Apply that security scheme globally"
// After this change, Swagger UI will show a padlock button (🔒) where you
// can paste your JWT token to test protected endpoints.
//
// NOTE: Swashbuckle 10 uses Microsoft.OpenApi v2.x which changed the API:
// - Types moved from Microsoft.OpenApi.Models → Microsoft.OpenApi namespace
// - OpenApiSecuritySchemeReference replaces the old Reference property pattern
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hospital Management System API",
        Version = "v1",
        Description = "Enterprise Hospital Management System - Built with ASP.NET Core 10, Clean Architecture, and JWT Authentication"
    });

    // Step 1: Define the JWT Bearer security scheme
    // This tells Swagger UI to show a padlock icon and an "Authorize" dialog
    // Note: In Swashbuckle 10 / OpenApi v2, AddSecurityDefinition takes IOpenApiSecurityScheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token.\n\nGet it from POST /api/v1/auth/login"
    });

    // Step 2: Require Bearer auth for all endpoints by default
    // In Swashbuckle 10 / OpenApi v2, AddSecurityRequirement takes a Func<OpenApiDocument, OpenApiSecurityRequirement>
    options.AddSecurityRequirement(doc =>
    {
        var requirement = new OpenApiSecurityRequirement();
        requirement.Add(new OpenApiSecuritySchemeReference("Bearer"), new List<string>());
        return requirement;
    });
});

// Register our three Clean Architecture layers
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// HttpContextAccessor allows us to read the current HTTP request's user/token
// from inside services (used by CurrentUserService for audit fields)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Hospital.Application.Services.Interfaces.ICurrentUserService, Hospital.API.Services.CurrentUserService>();

// ─────────────────────────────────────────────────────────────────────────────
// CORS (Cross-Origin Resource Sharing)
// When your React admin panel (running on port 3000) calls this API (port 5168),
// the browser blocks it by default as a security measure.
// CORS policy tells the browser: "These origins are trusted, allow their requests."
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()    // In production, replace with specific origins
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// MIDDLEWARE PIPELINE
// Middleware is a chain of components that process HTTP requests in order.
// Think of it like airport security: request goes through each checkpoint.
// ORDER MATTERS — each piece of middleware only runs if the previous one calls next()
// 
// The correct order:
// 1. Exception handling (must be first to catch errors from everything below)
// 2. HTTPS redirect
// 3. Static files (if any)
// 4. CORS (must be before auth)
// 5. Authentication (who are you?)
// 6. Authorization (what are you allowed to do?)
// 7. Controller routing
// ─────────────────────────────────────────────────────────────────────────────

// 1. Global exception handler — catches unhandled exceptions from all other middleware
app.UseMiddleware<Hospital.API.Middleware.ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Swagger only available in development for security
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospital Management System API v1");
        c.RoutePrefix = string.Empty; // Swagger at root URL: http://localhost:5168/
    });
}

// 2. Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// 3. CORS — must come before Authentication
app.UseCors("AllowAll");

// 4. Authentication — validates the JWT token
// This reads the Authorization header, validates the JWT, and sets HttpContext.User
app.UseAuthentication();

// 5. Authorization — checks if the authenticated user has the right role/policy
// This runs AFTER authentication because you need to know WHO they are first
app.UseAuthorization();

app.MapControllers();

app.Run();

// ─────────────────────────────────────────────────────────────────────────────
// INTEGRATION TEST HOOK
//
// WHY THIS EXISTS:
// In .NET 6+, top-level statements in Program.cs generate an implicit internal
// class named "Program". The integration test project (IntegrationTests.csproj)
// is a SEPARATE assembly — it cannot access internal types.
//
// "partial class Program" makes the compiler merge this declaration with the
// auto-generated one and gives us control over its accessibility.
// "public" makes it visible to the test assembly.
//
// WebApplicationFactory<Program> in the test project needs to reference this
// class to know which Program to boot. Without this line, you get:
//   error CS0122: 'Program' is inaccessible due to its protection level
// ─────────────────────────────────────────────────────────────────────────────
public partial class Program { }
