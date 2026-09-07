using AutoMapper;
using Hospital.Application.DTOs.Laboratory;
using Hospital.Domain.Entities;
using System.Linq;

namespace Hospital.Application.Mappings
{
    public class LaboratoryProfile : Profile
    {
        public LaboratoryProfile()
        {
            // LabTest mappings
            CreateMap<LabTest, LabTestDto>();
            CreateMap<CreateLabTestDto, LabTest>();
            CreateMap<UpdateLabTestDto, LabTest>();

            // LabOrder mappings
            CreateMap<LabOrder, LabOrderDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Patient != null ? $"{src.Patient.FirstName} {src.Patient.LastName}" : string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Doctor != null ? $"Dr. {src.Doctor.FirstName} {src.Doctor.LastName}" : string.Empty))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.Items != null ? src.Items.Sum(i => i.LabTest != null ? i.LabTest.Price : 0) : 0))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<LabOrderItem, LabOrderItemDto>()
                .ForMember(dest => dest.LabTestName, opt => opt.MapFrom(src => src.LabTest != null ? src.LabTest.Name : string.Empty))
                .ForMember(dest => dest.LabTestCode, opt => opt.MapFrom(src => src.LabTest != null ? src.LabTest.Code : string.Empty))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.LabTest != null ? src.LabTest.Price : 0));

            CreateMap<CreateLabOrderDto, LabOrder>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<CreateLabOrderItemDto, LabOrderItem>();
        }
    }
}
