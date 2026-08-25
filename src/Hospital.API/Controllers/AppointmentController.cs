using Hospital.Application.DTOs.Appointment;
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
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of appointments.
        /// Query: patientId, doctorId, status (0-3), fromDate, toDate, pageNumber, pageSize, sortBy
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AppointmentDto>>>> GetAppointments(
            [FromQuery] AppointmentQueryParams queryParams)
        {
            var result = await _appointmentService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<AppointmentDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} appointments"));
        }

        /// <summary>Returns a single appointment by ID with patient and doctor names.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<AppointmentDto>>> GetAppointmentById(Guid id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            return Ok(ApiResponse<AppointmentDto>.SuccessResult(
                appointment, "Appointment retrieved successfully"));
        }

        /// <summary>Books a new appointment.</summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Receptionist},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<AppointmentDto>>> CreateAppointment(
            [FromBody] CreateAppointmentDto createAppointmentDto)
        {
            var created = await _appointmentService.CreateAppointmentAsync(createAppointmentDto);
            return CreatedAtAction(
                nameof(GetAppointmentById),
                new { id = created.Id },
                ApiResponse<AppointmentDto>.SuccessResult(created, "Appointment booked successfully"));
        }

        /// <summary>Updates an appointment — reschedule, change status, add notes.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Receptionist},{Roles.Doctor}")]
        public async Task<ActionResult<ApiResponse>> UpdateAppointment(
            Guid id, [FromBody] UpdateAppointmentDto updateAppointmentDto)
        {
            if (id != updateAppointmentDto.Id)
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));

            await _appointmentService.UpdateAppointmentAsync(updateAppointmentDto);
            return Ok(ApiResponse.SuccessResult("Appointment updated successfully"));
        }

        /// <summary>Soft-deletes an appointment. Admin only.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = Roles.AdminAndAbove)]
        public async Task<ActionResult<ApiResponse>> DeleteAppointment(Guid id)
        {
            await _appointmentService.DeleteAppointmentAsync(id);
            return Ok(ApiResponse.SuccessResult("Appointment deleted successfully"));
        }
    }
}
