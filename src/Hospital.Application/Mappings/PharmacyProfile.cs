using AutoMapper;
using Hospital.Application.DTOs.Pharmacy;
using Hospital.Domain.Entities;

namespace Hospital.Application.Mappings
{
    public class PharmacyProfile : Profile
    {
        public PharmacyProfile()
        {
            // Medication mappings
            CreateMap<Medication, MedicationDto>();
            CreateMap<CreateMedicationDto, Medication>();
            CreateMap<UpdateMedicationDto, Medication>();

            // Prescription mappings
            CreateMap<Prescription, PrescriptionDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => $"{src.Patient.FirstName} {src.Patient.LastName}"))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => $"Dr. {src.Doctor.FirstName} {src.Doctor.LastName}"))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<PrescriptionItem, PrescriptionItemDto>()
                .ForMember(dest => dest.MedicationName, opt => opt.MapFrom(src => src.Medication.Name));

            CreateMap<CreatePrescriptionDto, Prescription>()
                .ForMember(dest => dest.PrescriptionDate, opt => opt.MapFrom(src => src.PrescriptionDate ?? System.DateTime.UtcNow))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<CreatePrescriptionItemDto, PrescriptionItem>();
        }
    }
}
