using Hospital.Application.DTOs.Appointment;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    /// <summary>
    /// Manages patient appointments.
    /// 
    /// Authorization strategy:
    ///   - View: Doctors, Nurses, Receptionists, Admins (they need to see the schedule)
    ///   - Book/Create: Receptionist, Patient (self-booking), Admin
    ///   - Update (reschedule/cancel): Receptionist, Admin
    ///   - Delete: Admin only
    /// </summary>
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
        /// Returns all appointments with patient and doctor names populated.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Receptionist")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetAllAppointments()
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            return Ok(ApiResponse<IEnumerable<AppointmentDto>>.SuccessResult(appointments, "Appointments retrieved successfully"));
        }

        /// <summary>
        /// Returns a single appointment by ID with full patient and doctor details.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Receptionist,Patient")]
        public async Task<ActionResult<ApiResponse<AppointmentDto>>> GetAppointmentById(Guid id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            return Ok(ApiResponse<AppointmentDto>.SuccessResult(appointment, "Appointment retrieved successfully"));
        }

        /// <summary>
        /// Books a new appointment.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Receptionist,Patient")]
        public async Task<ActionResult<ApiResponse<AppointmentDto>>> CreateAppointment([FromBody] CreateAppointmentDto createAppointmentDto)
        {
            var createdAppointment = await _appointmentService.CreateAppointmentAsync(createAppointmentDto);
            return CreatedAtAction(
                nameof(GetAppointmentById),
                new { id = createdAppointment.Id },
                ApiResponse<AppointmentDto>.SuccessResult(createdAppointment, "Appointment booked successfully"));
        }

        /// <summary>
        /// Updates an appointment (reschedule, status change, add notes).
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,Receptionist,Doctor")]
        public async Task<ActionResult<ApiResponse>> UpdateAppointment(Guid id, [FromBody] UpdateAppointmentDto updateAppointmentDto)
        {
            if (id != updateAppointmentDto.Id)
            {
                return BadRequest(ApiResponse.FailResult("ID in URL does not match ID in request body."));
            }

            await _appointmentService.UpdateAppointmentAsync(updateAppointmentDto);
            return Ok(ApiResponse.SuccessResult("Appointment updated successfully"));
        }

        /// <summary>
        /// Soft-deletes an appointment. Admin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse>> DeleteAppointment(Guid id)
        {
            await _appointmentService.DeleteAppointmentAsync(id);
            return Ok(ApiResponse.SuccessResult("Appointment deleted successfully"));
        }
    }
}
