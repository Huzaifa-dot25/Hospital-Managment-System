using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hospital.Application.DTOs.Appointment;

namespace Hospital.Application.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync();
        Task<AppointmentDto> GetAppointmentByIdAsync(Guid id);
        Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto);
        Task UpdateAppointmentAsync(UpdateAppointmentDto updateAppointmentDto);
        Task DeleteAppointmentAsync(Guid id);
    }
}
