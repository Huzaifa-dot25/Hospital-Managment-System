using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hospital.Application.DTOs.Patient;

namespace Hospital.Application.Services.Interfaces
{
    public interface IPatientService
    {
        Task<IEnumerable<PatientDto>> GetAllPatientsAsync();
        Task<PatientDto> GetPatientByIdAsync(Guid id);
        Task<PatientDto> CreatePatientAsync(CreatePatientDto createPatientDto);
        Task UpdatePatientAsync(UpdatePatientDto updatePatientDto);
        Task DeletePatientAsync(Guid id);
    }
}
