using Hospital.Application.DTOs.MedicalRecord;
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
    public class MedicalRecordController : ControllerBase
    {
        private readonly IMedicalRecordService _medicalRecordService;

        public MedicalRecordController(IMedicalRecordService medicalRecordService)
        {
            _medicalRecordService = medicalRecordService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of medical records.
        /// Clinical staff only (SuperAdmin, Admin, Doctor, Nurse).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<MedicalRecordDto>>>> GetMedicalRecords(
            [FromQuery] MedicalRecordQueryParams queryParams)
        {
            var result = await _medicalRecordService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<MedicalRecordDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} medical records"));
        }

        /// <summary>
        /// Returns a single medical record by ID with patient and doctor names.
        /// Clinical staff and patient.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<MedicalRecordDto>>> GetMedicalRecordById(Guid id)
        {
            var record = await _medicalRecordService.GetMedicalRecordByIdAsync(id);
            return Ok(ApiResponse<MedicalRecordDto>.SuccessResult(
                record, "Medical record retrieved successfully"));
        }

        /// <summary>
        /// Creates a new medical record.
        /// Doctor or Admin only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
        public async Task<ActionResult<ApiResponse<MedicalRecordDto>>> CreateMedicalRecord(
            [FromBody] CreateMedicalRecordDto createDto)
        {
            var created = await _medicalRecordService.CreateMedicalRecordAsync(createDto);
            return CreatedAtAction(
                nameof(GetMedicalRecordById),
                new { id = created.Id },
                ApiResponse<MedicalRecordDto>.SuccessResult(created, "Medical record created successfully"));
        }

        /// <summary>
        /// Updates a medical record's clinical findings, treatment, or notes.
        /// Doctor or Admin only.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
        public async Task<ActionResult<ApiResponse>> UpdateMedicalRecord(
            Guid id, [FromBody] UpdateMedicalRecordDto updateDto)
        {
            if (id != updateDto.Id)
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));

            await _medicalRecordService.UpdateMedicalRecordAsync(updateDto);
            return Ok(ApiResponse.SuccessResult("Medical record updated successfully"));
        }

        /// <summary>
        /// Soft-deletes a medical record.
        /// Admin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = Roles.AdminAndAbove)]
        public async Task<ActionResult<ApiResponse>> DeleteMedicalRecord(Guid id)
        {
            await _medicalRecordService.DeleteMedicalRecordAsync(id);
            return Ok(ApiResponse.SuccessResult("Medical record deleted successfully"));
        }
    }
}
