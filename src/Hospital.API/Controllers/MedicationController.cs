using Hospital.Application.DTOs.Pharmacy;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Constants;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class MedicationController : ControllerBase
    {
        private readonly IMedicationService _medicationService;

        public MedicationController(IMedicationService medicationService)
        {
            _medicationService = medicationService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of medications.
        /// Available to all hospital staff.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Roles.AnyStaff)]
        public async Task<ActionResult<ApiResponse<PagedResponse<MedicationDto>>>> GetMedications(
            [FromQuery] MedicationQueryParams queryParams)
        {
            var result = await _medicationService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<MedicationDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} medications"));
        }

        /// <summary>
        /// Returns a single medication by ID.
        /// Available to all hospital staff.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = Roles.AnyStaff)]
        public async Task<ActionResult<ApiResponse<MedicationDto>>> GetMedicationById(Guid id)
        {
            var medication = await _medicationService.GetMedicationByIdAsync(id);
            return Ok(ApiResponse<MedicationDto>.SuccessResult(
                medication, "Medication retrieved successfully"));
        }

        /// <summary>
        /// Returns a list of medications with stock at or below the threshold.
        /// Pharmacist and Admin only.
        /// </summary>
        [HttpGet("low-stock")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Pharmacist}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<MedicationDto>>>> GetLowStock(
            [FromQuery] int threshold = 10)
        {
            var medications = await _medicationService.GetLowStockAsync(threshold);
            return Ok(ApiResponse<IReadOnlyList<MedicationDto>>.SuccessResult(
                medications, $"Found {medications.Count} low-stock medications"));
        }

        /// <summary>
        /// Adds a new medication to the inventory.
        /// Pharmacist and Admin only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Pharmacist}")]
        public async Task<ActionResult<ApiResponse<MedicationDto>>> CreateMedication(
            [FromBody] CreateMedicationDto createDto)
        {
            var created = await _medicationService.CreateMedicationAsync(createDto);
            return CreatedAtAction(
                nameof(GetMedicationById),
                new { id = created.Id },
                ApiResponse<MedicationDto>.SuccessResult(created, "Medication added successfully"));
        }

        /// <summary>
        /// Updates a medication's details and stock.
        /// Pharmacist and Admin only.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Pharmacist}")]
        public async Task<ActionResult<ApiResponse>> UpdateMedication(
            Guid id, [FromBody] UpdateMedicationDto updateDto)
        {
            if (id != updateDto.Id)
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));

            await _medicationService.UpdateMedicationAsync(updateDto);
            return Ok(ApiResponse.SuccessResult("Medication updated successfully"));
        }

        /// <summary>
        /// Soft-deletes a medication from the catalog.
        /// Admin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = Roles.AdminAndAbove)]
        public async Task<ActionResult<ApiResponse>> DeleteMedication(Guid id)
        {
            await _medicationService.DeleteMedicationAsync(id);
            return Ok(ApiResponse.SuccessResult("Medication deleted successfully"));
        }
    }
}
