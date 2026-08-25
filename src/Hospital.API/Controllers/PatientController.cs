using Hospital.Application.DTOs.Patient;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Constants;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of patients.
        /// Query: search, bloodGroup, gender, pageNumber, pageSize, sortBy, isDescending
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PatientDto>>>> GetPatients(
            [FromQuery] PatientQueryParams queryParams)
        {
            var result = await _patientService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<PatientDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} patients"));
        }

        /// <summary>Returns a single patient by ID.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist}")]
        public async Task<ActionResult<ApiResponse<PatientDto>>> GetPatientById(Guid id)
        {
            var patient = await _patientService.GetPatientByIdAsync(id);
            return Ok(ApiResponse<PatientDto>.SuccessResult(patient, "Patient retrieved successfully"));
        }

        /// <summary>Registers a new patient.</summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Receptionist}")]
        public async Task<ActionResult<ApiResponse<PatientDto>>> CreatePatient(
            [FromBody] CreatePatientDto createPatientDto)
        {
            var created = await _patientService.CreatePatientAsync(createPatientDto);
            return CreatedAtAction(
                nameof(GetPatientById),
                new { id = created.Id },
                ApiResponse<PatientDto>.SuccessResult(created, "Patient registered successfully"));
        }

        /// <summary>Updates an existing patient's information.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Receptionist}")]
        public async Task<ActionResult<ApiResponse>> UpdatePatient(
            Guid id, [FromBody] UpdatePatientDto updatePatientDto)
        {
            if (id != updatePatientDto.Id)
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));

            await _patientService.UpdatePatientAsync(updatePatientDto);
            return Ok(ApiResponse.SuccessResult("Patient updated successfully"));
        }

        /// <summary>Soft-deletes a patient.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = Roles.AdminAndAbove)]
        public async Task<ActionResult<ApiResponse>> DeletePatient(Guid id)
        {
            await _patientService.DeletePatientAsync(id);
            return Ok(ApiResponse.SuccessResult("Patient deleted successfully"));
        }
    }
}
