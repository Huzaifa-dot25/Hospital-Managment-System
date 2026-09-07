using AutoMapper;
using Hospital.Application.DTOs.MedicalRecord;
using Hospital.Domain.Entities;

namespace Hospital.Application.Mappings
{
    public class MedicalRecordProfile : Profile
    {
        public MedicalRecordProfile()
        {
            CreateMap<MedicalRecord, MedicalRecordDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => $"{src.Patient.FirstName} {src.Patient.LastName}"))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => $"Dr. {src.Doctor.FirstName} {src.Doctor.LastName}"));

            CreateMap<CreateMedicalRecordDto, MedicalRecord>()
                .ForMember(dest => dest.RecordDate, opt => opt.MapFrom(src => src.RecordDate ?? System.DateTime.UtcNow));

            CreateMap<UpdateMedicalRecordDto, MedicalRecord>();
        }
    }
}
