using Hospital.Application.DTOs.Patient;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    /// <summary>
    /// Manages patient records.
    /// 
    /// Authorization strategy:
    ///   - Reading patients: Doctors, Nurses, Receptionists, and Admins can all view
    ///   - Creating patients: Receptionist or Admin registers new patients at the front desk
    ///   - Updating/Deleting: Admin only (sensitive data)
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize] // ← All endpoints in this controller require a valid JWT token by default
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        /// <summary>
        /// Retrieves all patients (non-deleted).
        /// Accessible by: Admin, Doctor, Nurse, Receptionist
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Receptionist")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PatientDto>>>> GetAllPatients()
        {
            var patients = await _patientService.GetAllPatientsAsync();
            return Ok(ApiResponse<IEnumerable<PatientDto>>.SuccessResult(patients, "Patients retrieved successfully"));
        }

        /// <summary>
        /// Retrieves a single patient by their unique ID.
        /// Accessible by: Admin, Doctor, Nurse, Receptionist
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Receptionist")]
        public async Task<ActionResult<ApiResponse<PatientDto>>> GetPatientById(Guid id)
        {
            var patient = await _patientService.GetPatientByIdAsync(id);
            return Ok(ApiResponse<PatientDto>.SuccessResult(patient, "Patient retrieved successfully"));
        }

        /// <summary>
        /// Registers a new patient in the system.
        /// Accessible by: Admin, Receptionist
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Receptionist")]
        public async Task<ActionResult<ApiResponse<PatientDto>>> CreatePatient([FromBody] CreatePatientDto createPatientDto)
        {
            var createdPatient = await _patientService.CreatePatientAsync(createPatientDto);
            return CreatedAtAction(
                nameof(GetPatientById),
                new { id = createdPatient.Id },
                ApiResponse<PatientDto>.SuccessResult(createdPatient, "Patient created successfully"));
        }

        /// <summary>
        /// Updates an existing patient's information.
        /// Accessible by: Admin, Receptionist
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,Receptionist")]
        public async Task<ActionResult<ApiResponse>> UpdatePatient(Guid id, [FromBody] UpdatePatientDto updatePatientDto)
        {
            if (id != updatePatientDto.Id)
            {
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));
            }

            await _patientService.UpdatePatientAsync(updatePatientDto);
            return Ok(ApiResponse.SuccessResult("Patient updated successfully"));
        }

        /// <summary>
        /// Soft-deletes a patient (sets IsDeleted = true, does not remove from DB).
        /// Accessible by: Admin only
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse>> DeletePatient(Guid id)
        {
            await _patientService.DeletePatientAsync(id);
            return Ok(ApiResponse.SuccessResult("Patient deleted successfully"));
        }
    }
}
