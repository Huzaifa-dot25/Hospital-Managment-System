using AutoMapper;
using Hospital.Domain.Entities;
using Hospital.Application.DTOs.Department;

namespace Hospital.Application.Mappings
{
    public class DepartmentProfile : Profile
    {
        public DepartmentProfile()
        {
            // Map from Entity to DTO
            CreateMap<Department, DepartmentDto>();

            // Map from Create DTO to Entity
            CreateMap<CreateDepartmentDto, Department>();

            // Map from Update DTO to Entity
            CreateMap<UpdateDepartmentDto, Department>();
        }
    }
}
