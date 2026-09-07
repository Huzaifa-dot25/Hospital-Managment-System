using Hospital.Application.DTOs.Pharmacy;
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
    public class PrescriptionController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;

        public PrescriptionController(IPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of prescriptions.
        /// Available to clinical and pharmacy staff.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Pharmacist}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PrescriptionDto>>>> GetPrescriptions(
            [FromQuery] PrescriptionQueryParams queryParams)
        {
            var result = await _prescriptionService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<PrescriptionDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} prescriptions"));
        }

        /// <summary>
        /// Returns a single prescription with items and medication details by ID.
        /// Available to clinical staff, pharmacy staff, and patient.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Pharmacist},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetPrescriptionById(Guid id)
        {
            var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
            return Ok(ApiResponse<PrescriptionDto>.SuccessResult(
                prescription, "Prescription retrieved successfully"));
        }

        /// <summary>
        /// Creates a new prescription with medications for a patient.
        /// Doctor and Admin only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CreatePrescription(
            [FromBody] CreatePrescriptionDto createDto)
        {
            var created = await _prescriptionService.CreatePrescriptionAsync(createDto);
            return CreatedAtAction(
                nameof(GetPrescriptionById),
                new { id = created.Id },
                ApiResponse<PrescriptionDto>.SuccessResult(created, "Prescription created successfully"));
        }

        /// <summary>
        /// Dispenses a prescription, deducting medication quantities from stock.
        /// Pharmacist and Admin only.
        /// </summary>
        [HttpPost("{id:guid}/dispense")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Pharmacist}")]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> DispensePrescription(
            Guid id, [FromBody] DispensePrescriptionDto dispenseDto)
        {
            try
            {
                var dispensed = await _prescriptionService.DispensePrescriptionAsync(id, dispenseDto);
                return Ok(ApiResponse<PrescriptionDto>.SuccessResult(
                    dispensed, "Prescription dispensed successfully and stock updated."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.FailResult(ex.Message));
            }
        }

        /// <summary>
        /// Cancels a prescription.
        /// Prescribing Doctor or Admin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
        public async Task<ActionResult<ApiResponse>> CancelPrescription(Guid id)
        {
            try
            {
                await _prescriptionService.CancelPrescriptionAsync(id);
                return Ok(ApiResponse.SuccessResult("Prescription cancelled successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.FailResult(ex.Message));
            }
        }
    }
}
