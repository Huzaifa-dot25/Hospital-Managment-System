using Hospital.Application.DTOs.Doctor;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    /// <summary>
    /// Manages doctor profiles.
    /// Only admins can create/update/delete doctors (HR operations).
    /// Viewing doctors is open to most staff roles.
    /// </summary>
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
        /// Retrieves all doctors with their department information.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Receptionist,Patient")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetAllDoctors()
        {
            var doctors = await _doctorService.GetAllDoctorsAsync();
            return Ok(ApiResponse<IEnumerable<DoctorDto>>.SuccessResult(doctors, "Doctors retrieved successfully"));
        }

        /// <summary>
        /// Retrieves a single doctor by ID, including their department name.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Receptionist,Patient")]
        public async Task<ActionResult<ApiResponse<DoctorDto>>> GetDoctorById(Guid id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            return Ok(ApiResponse<DoctorDto>.SuccessResult(doctor, "Doctor retrieved successfully"));
        }

        /// <summary>
        /// Registers a new doctor. Admin-only operation (HR function).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse<DoctorDto>>> CreateDoctor([FromBody] CreateDoctorDto createDoctorDto)
        {
            var createdDoctor = await _doctorService.CreateDoctorAsync(createDoctorDto);
            return CreatedAtAction(
                nameof(GetDoctorById),
                new { id = createdDoctor.Id },
                ApiResponse<DoctorDto>.SuccessResult(createdDoctor, "Doctor created successfully"));
        }

        /// <summary>
        /// Updates a doctor's profile. Admin-only.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse>> UpdateDoctor(Guid id, [FromBody] UpdateDoctorDto updateDoctorDto)
        {
            if (id != updateDoctorDto.Id)
            {
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));
            }

            await _doctorService.UpdateDoctorAsync(updateDoctorDto);
            return Ok(ApiResponse.SuccessResult("Doctor updated successfully"));
        }

        /// <summary>
        /// Soft-deletes a doctor. Admin-only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse>> DeleteDoctor(Guid id)
        {
            await _doctorService.DeleteDoctorAsync(id);
            return Ok(ApiResponse.SuccessResult("Doctor deleted successfully"));
        }
    }
}
