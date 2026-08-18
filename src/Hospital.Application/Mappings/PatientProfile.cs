using AutoMapper;
using Hospital.Application.DTOs.Patient;
using Hospital.Domain.Entities;

namespace Hospital.Application.Mappings
{
    public class PatientProfile : Profile
    {
        public PatientProfile()
        {
            CreateMap<Patient, PatientDto>();
            CreateMap<CreatePatientDto, Patient>();
            CreateMap<UpdatePatientDto, Patient>();
        }
    }
}
