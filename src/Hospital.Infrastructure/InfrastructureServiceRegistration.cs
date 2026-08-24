using Hospital.Domain.Entities.Identity;
using Hospital.Infrastructure.Authentication;
using Hospital.Infrastructure.Services;
using Hospital.Application.Services.Interfaces;
using Hospital.Persistence.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace Hospital.Infrastructure
{
    /// <summary>
    /// Registers infrastructure services: JWT, AuthService, and ASP.NET Identity.
    /// 
    /// Identity configuration lives here (not in Persistence) because:
    /// - Identity involves authentication policy decisions (password rules, lockout)
    ///   which are infrastructure/security concerns.
    /// - .AddEntityFrameworkStores needs ApplicationDbContext, so we add a project
    ///   reference from Infrastructure to Persistence for this purpose.
    /// - This is a pragmatic architectural decision — the alternative (moving to Program.cs)
    ///   would clutter the startup file.
    /// </summary>
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind JwtOptions from appsettings.json
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

            // Register AuthService
            services.AddScoped<IAuthService, AuthService>();

            // ─────────────────────────────────────────────────────────────
            // ASP.NET CORE IDENTITY CONFIGURATION
            // This registers all Identity services: UserManager, RoleManager,
            // SignInManager, password hasher, validators, token providers.
            // ─────────────────────────────────────────────────────────────
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;

                // Lock account after 5 failed logins for 15 minutes
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // ─────────────────────────────────────────────────────────────
            // JWT BEARER AUTHENTICATION
            // ─────────────────────────────────────────────────────────────
            var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions!.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }
    }
}
