using Hospital.Application.DTOs.Patient;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    /// <summary>
    /// Defines all use cases for the Patient module.
    ///
    /// Two ways to get patients:
    ///   GetPagedAsync  — for list endpoints (paginated, filtered, sorted)
    ///   GetByIdAsync   — for single-record endpoints
    ///
    /// The old GetAllPatientsAsync is removed. Every list goes through pagination.
    /// Returning unbounded lists is a production anti-pattern.
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// Returns a paginated, filtered, sorted page of patients.
        /// Used by GET /api/v1/patient
        /// </summary>
        Task<PagedResponse<PatientDto>> GetPagedAsync(PatientQueryParams queryParams);

        /// <summary>Returns a single patient by ID.</summary>
        Task<PatientDto> GetPatientByIdAsync(Guid id);

        /// <summary>Registers a new patient and returns the created record.</summary>
        Task<PatientDto> CreatePatientAsync(CreatePatientDto createPatientDto);

        /// <summary>Updates an existing patient's information.</summary>
        Task UpdatePatientAsync(UpdatePatientDto updatePatientDto);

        /// <summary>Soft-deletes a patient (sets IsDeleted = true).</summary>
        Task DeletePatientAsync(Guid id);
    }
}
