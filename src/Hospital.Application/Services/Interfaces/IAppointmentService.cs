using Hospital.Application.DTOs.Appointment;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    /// <summary>
    /// Defines all use cases for the Appointment module.
    /// </summary>
    public interface IAppointmentService
    {
        /// <summary>
        /// Returns a paginated, filtered page of appointments.
        /// Supports filtering by patient, doctor, status, and date range.
        /// Used by GET /api/v1/appointment
        /// </summary>
        Task<PagedResponse<AppointmentDto>> GetPagedAsync(AppointmentQueryParams queryParams);

        /// <summary>Returns a single appointment by ID with patient and doctor names.</summary>
        Task<AppointmentDto> GetAppointmentByIdAsync(Guid id);

        /// <summary>Books a new appointment.</summary>
        Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto);

        /// <summary>Updates an appointment (reschedule, status change, add notes).</summary>
        Task UpdateAppointmentAsync(UpdateAppointmentDto updateAppointmentDto);

        /// <summary>Soft-deletes an appointment.</summary>
        Task DeleteAppointmentAsync(Guid id);
    }
}
