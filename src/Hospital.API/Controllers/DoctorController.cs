using Hospital.Application.DTOs.Doctor;
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
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public DoctorController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of doctors with department names.
        /// Query: search, specialization, departmentId, pageNumber, pageSize, sortBy, isDescending
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<DoctorDto>>>> GetDoctors(
            [FromQuery] DoctorQueryParams queryParams)
        {
            var result = await _doctorService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<DoctorDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} doctors"));
        }

        /// <summary>Returns a single doctor by ID with department name.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<DoctorDto>>> GetDoctorById(Guid id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            return Ok(ApiResponse<DoctorDto>.SuccessResult(doctor, "Doctor retrieved successfully"));
        }

        /// <summary>Registers a new doctor. Admin only.</summary>
        [HttpPost]
        [Authorize(Roles = Roles.AdminAndAbove)]
        public async Task<ActionResult<ApiResponse<DoctorDto>>> CreateDoctor(
            [FromBody] CreateDoctorDto createDoctorDto)
        {
            var created = await _doctorService.CreateDoctorAsync(createDoctorDto);
            return CreatedAtAction(
                nameof(GetDoctorById),
                new { id = created.Id },
                ApiResponse<DoctorDto>.SuccessResult(created, "Doctor registered successfully"));
        }

        /// <summary>Updates a doctor's profile. Admin only.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = Roles.AdminAndAbove)]
        public async Task<ActionResult<ApiResponse>> UpdateDoctor(
            Guid id, [FromBody] UpdateDoctorDto updateDoctorDto)
        {
            if (id != updateDoctorDto.Id)
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));

            await _doctorService.UpdateDoctorAsync(updateDoctorDto);
            return Ok(ApiResponse.SuccessResult("Doctor updated successfully"));
        }

        /// <summary>Soft-deletes a doctor. Admin only.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = Roles.AdminAndAbove)]
        public async Task<ActionResult<ApiResponse>> DeleteDoctor(Guid id)
        {
            await _doctorService.DeleteDoctorAsync(id);
            return Ok(ApiResponse.SuccessResult("Doctor deleted successfully"));
        }
    }
}
