using Hospital.Application.DTOs.Laboratory;
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
    public class LabTestController : ControllerBase
    {
        private readonly ILabTestService _labTestService;

        public LabTestController(ILabTestService labTestService)
        {
            _labTestService = labTestService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of laboratory tests.
        /// Available to all hospital staff.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Roles.AnyStaff)]
        public async Task<ActionResult<ApiResponse<PagedResponse<LabTestDto>>>> GetLabTests(
            [FromQuery] LabTestQueryParams queryParams)
        {
            var result = await _labTestService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<LabTestDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} lab tests"));
        }

        /// <summary>
        /// Returns a single lab test by ID.
        /// Available to all hospital staff.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = Roles.AnyStaff)]
        public async Task<ActionResult<ApiResponse<LabTestDto>>> GetLabTestById(Guid id)
        {
            var test = await _labTestService.GetByIdAsync(id);
            return Ok(ApiResponse<LabTestDto>.SuccessResult(
                test, "Lab test retrieved successfully"));
        }

        /// <summary>
        /// Adds a new lab test to the catalog.
        /// LabTechnician and Admin only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.LabTechnician}")]
        public async Task<ActionResult<ApiResponse<LabTestDto>>> CreateLabTest(
            [FromBody] CreateLabTestDto createDto)
        {
            var created = await _labTestService.CreateAsync(createDto);
            return CreatedAtAction(
                nameof(GetLabTestById),
                new { id = created.Id },
                ApiResponse<LabTestDto>.SuccessResult(created, "Lab test created successfully"));
        }

        /// <summary>
        /// Updates an existing lab test in the catalog.
        /// LabTechnician and Admin only.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.LabTechnician}")]
        public async Task<ActionResult<ApiResponse>> UpdateLabTest(
            Guid id, [FromBody] UpdateLabTestDto updateDto)
        {
            if (id != updateDto.Id)
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));

            await _labTestService.UpdateAsync(updateDto);
            return Ok(ApiResponse.SuccessResult("Lab test updated successfully"));
        }

        /// <summary>
        /// Soft-deletes a lab test from the catalog.
        /// Admin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = Roles.AdminAndAbove)]
        public async Task<ActionResult<ApiResponse>> DeleteLabTest(Guid id)
        {
            await _labTestService.DeleteAsync(id);
            return Ok(ApiResponse.SuccessResult("Lab test deleted successfully"));
        }
    }
}
