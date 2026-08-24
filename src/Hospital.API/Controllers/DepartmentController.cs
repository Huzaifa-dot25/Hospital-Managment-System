using Hospital.Application.DTOs.Department;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    /// <summary>
    /// Manages hospital departments (Cardiology, Neurology, Orthopedics, etc.)
    /// Departments are mostly read-only for most staff.
    /// Only SuperAdmin/Admin can create or modify them.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        /// <summary>
        /// Returns all hospital departments.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Receptionist,Patient,Pharmacist,LabTechnician")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentDto>>>> GetAllDepartments()
        {
            var departments = await _departmentService.GetAllDepartmentsAsync();
            return Ok(ApiResponse<IEnumerable<DepartmentDto>>.SuccessResult(departments, "Departments retrieved successfully"));
        }

        /// <summary>
        /// Returns a single department by ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Receptionist,Patient,Pharmacist,LabTechnician")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartmentById(Guid id)
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id);
            return Ok(ApiResponse<DepartmentDto>.SuccessResult(department, "Department retrieved successfully"));
        }

        /// <summary>
        /// Creates a new department. SuperAdmin/Admin only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment([FromBody] CreateDepartmentDto createDepartmentDto)
        {
            var createdDepartment = await _departmentService.CreateDepartmentAsync(createDepartmentDto);
            return CreatedAtAction(
                nameof(GetDepartmentById),
                new { id = createdDepartment.Id },
                ApiResponse<DepartmentDto>.SuccessResult(createdDepartment, "Department created successfully"));
        }

        /// <summary>
        /// Updates a department. SuperAdmin/Admin only.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse>> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentDto updateDepartmentDto)
        {
            if (id != updateDepartmentDto.Id)
            {
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));
            }

            await _departmentService.UpdateDepartmentAsync(updateDepartmentDto);
            return Ok(ApiResponse.SuccessResult("Department updated successfully"));
        }

        /// <summary>
        /// Soft-deletes a department. SuperAdmin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<ApiResponse>> DeleteDepartment(Guid id)
        {
            await _departmentService.DeleteDepartmentAsync(id);
            return Ok(ApiResponse.SuccessResult("Department deleted successfully"));
        }
    }
}
