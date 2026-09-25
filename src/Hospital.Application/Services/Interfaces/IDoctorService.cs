using Hospital.Application.DTOs.Doctor;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    /// <summary>
    /// Defines all use cases for the Doctor module.
    /// </summary>
    public interface IDoctorService
    {
        /// <summary>
        /// Returns a paginated, filtered, sorted page of doctors with department names.
        /// Used by GET /api/v1/doctor
        /// </summary>
        Task<PagedResponse<DoctorDto>> GetPagedAsync(DoctorQueryParams queryParams);

        /// <summary>Returns a single doctor by ID with department name.</summary>
        Task<DoctorDto> GetDoctorByIdAsync(Guid id);

        /// <summary>Registers a new doctor and returns the created record.</summary>
        Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto);

        /// <summary>Updates a doctor's profile.</summary>
        Task UpdateDoctorAsync(UpdateDoctorDto updateDoctorDto);

        /// <summary>Soft-deletes a doctor.</summary>
        Task DeleteDoctorAsync(Guid id);

        /// <summary>Adds a working schedule to a doctor.</summary>
        Task<DoctorScheduleDto> AddScheduleAsync(Guid doctorId, CreateDoctorScheduleDto createScheduleDto);

        /// <summary>Gets all schedules for a doctor.</summary>
        Task<IEnumerable<DoctorScheduleDto>> GetDoctorSchedulesAsync(Guid doctorId);

        /// <summary>Removes a schedule.</summary>
        Task DeleteScheduleAsync(Guid doctorId, Guid scheduleId);
    }
}
