using System.Reflection;
using FluentValidation;
using Hospital.Application.Services;
using Hospital.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.Application
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // Register FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Register Application Services
            services.AddScoped<IDepartmentService, DepartmentService>();
            
            // We will add PatientService and DoctorService here later!

            return services;
        }
    }
}
