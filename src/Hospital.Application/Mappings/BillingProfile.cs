using AutoMapper;
using Hospital.Application.DTOs.Billing;
using Hospital.Domain.Entities;

namespace Hospital.Application.Mappings
{
    public class BillingProfile : Profile
    {
        public BillingProfile()
        {
            CreateMap<Invoice, InvoiceDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Patient != null ? $"{src.Patient.FirstName} {src.Patient.LastName}" : string.Empty))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments));

            CreateMap<InvoiceItem, InvoiceItemDto>()
                .ForMember(dest => dest.ItemType, opt => opt.MapFrom(src => src.ItemType.ToString()));

            CreateMap<Payment, PaymentDto>()
                .ForMember(dest => dest.Method, opt => opt.MapFrom(src => src.Method.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<CreateInvoiceDto, Invoice>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<CreateInvoiceItemDto, InvoiceItem>()
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.UnitPrice * src.Quantity));

            CreateMap<ProcessPaymentDto, Payment>();
        }
    }
}
