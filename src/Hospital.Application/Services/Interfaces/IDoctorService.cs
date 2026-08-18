using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hospital.Application.DTOs.Doctor;

namespace Hospital.Application.Services.Interfaces
{
    public interface IDoctorService
    {
        Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync();
        Task<DoctorDto> GetDoctorByIdAsync(Guid id);
        Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto);
        Task UpdateDoctorAsync(UpdateDoctorDto updateDoctorDto);
        Task DeleteDoctorAsync(Guid id);
    }
}
