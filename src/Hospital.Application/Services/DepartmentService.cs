using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Department;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;

namespace Hospital.Application.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateDepartmentDto> _createValidator;
        private readonly IValidator<UpdateDepartmentDto> _updateValidator;

        public DepartmentService(
            IUnitOfWork unitOfWork, 
            IMapper mapper,
            IValidator<CreateDepartmentDto> createValidator,
            IValidator<UpdateDepartmentDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
        {
            var departments = await _unitOfWork.Departments.GetAllAsync();
            return _mapper.Map<IEnumerable<DepartmentDto>>(departments);
        }

        public async Task<DepartmentDto> GetDepartmentByIdAsync(Guid id)
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id);
            if (department == null)
            {
                throw new NotFoundException(nameof(Department), id);
            }
            return _mapper.Map<DepartmentDto>(department);
        }

        public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto createDepartmentDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createDepartmentDto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var department = _mapper.Map<Department>(createDepartmentDto);
            department.CreatedDate = DateTime.UtcNow;

            await _unitOfWork.Departments.AddAsync(department);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DepartmentDto>(department);
        }

        public async Task UpdateDepartmentAsync(UpdateDepartmentDto updateDepartmentDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDepartmentDto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var departmentToUpdate = await _unitOfWork.Departments.GetByIdAsync(updateDepartmentDto.Id);
            if (departmentToUpdate == null)
            {
                throw new NotFoundException(nameof(Department), updateDepartmentDto.Id);
            }

            // Map updated fields
            _mapper.Map(updateDepartmentDto, departmentToUpdate);
            departmentToUpdate.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Departments.UpdateAsync(departmentToUpdate);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteDepartmentAsync(Guid id)
        {
            var departmentToDelete = await _unitOfWork.Departments.GetByIdAsync(id);
            if (departmentToDelete == null)
            {
                throw new NotFoundException(nameof(Department), id);
            }

            await _unitOfWork.Departments.DeleteAsync(departmentToDelete);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
