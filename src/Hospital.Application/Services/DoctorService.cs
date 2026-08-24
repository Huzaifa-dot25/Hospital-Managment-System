using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Doctor;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateDoctorDto> _createValidator;
        private readonly IValidator<UpdateDoctorDto> _updateValidator;

        public DoctorService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateDoctorDto> createValidator,
            IValidator<UpdateDoctorDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync()
        {
            // Use GetAllWithDepartmentAsync() instead of GetAllAsync()
            // so the Department navigation property is loaded.
            // Without this, src.Department.Name in DoctorProfile throws NullReferenceException.
            var doctors = await _unitOfWork.Doctors.GetAllWithDepartmentAsync();
            return _mapper.Map<IEnumerable<DoctorDto>>(doctors);
        }

        public async Task<DoctorDto> GetDoctorByIdAsync(Guid id)
        {
            // Use the eager-loading version here too
            var doctor = await _unitOfWork.Doctors.GetByIdWithDepartmentAsync(id);
            if (doctor == null)
            {
                throw new NotFoundException(nameof(Doctor), id);
            }
            return _mapper.Map<DoctorDto>(doctor);
        }

        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createDoctorDto);
            if (!validationResult.IsValid)
            {
                throw new AppValidationException(validationResult.Errors);
            }

            // Verify if department exists
            var department = await _unitOfWork.Departments.GetByIdAsync(createDoctorDto.DepartmentId);
            if (department == null)
            {
                throw new NotFoundException(nameof(Department), createDoctorDto.DepartmentId);
            }

            var doctor = _mapper.Map<Doctor>(createDoctorDto);
            doctor.CreatedDate = DateTime.UtcNow;

            await _unitOfWork.Doctors.AddAsync(doctor);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DoctorDto>(doctor);
        }

        public async Task UpdateDoctorAsync(UpdateDoctorDto updateDoctorDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDoctorDto);
            if (!validationResult.IsValid)
            {
                throw new AppValidationException(validationResult.Errors);
            }

            var doctorToUpdate = await _unitOfWork.Doctors.GetByIdAsync(updateDoctorDto.Id);
            if (doctorToUpdate == null)
            {
                throw new NotFoundException(nameof(Doctor), updateDoctorDto.Id);
            }

            // Verify if department exists if it was changed
            if (doctorToUpdate.DepartmentId != updateDoctorDto.DepartmentId)
            {
                var department = await _unitOfWork.Departments.GetByIdAsync(updateDoctorDto.DepartmentId);
                if (department == null)
                {
                    throw new NotFoundException(nameof(Department), updateDoctorDto.DepartmentId);
                }
            }

            _mapper.Map(updateDoctorDto, doctorToUpdate);
            doctorToUpdate.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Doctors.UpdateAsync(doctorToUpdate);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteDoctorAsync(Guid id)
        {
            var doctorToDelete = await _unitOfWork.Doctors.GetByIdAsync(id);
            if (doctorToDelete == null)
            {
                throw new NotFoundException(nameof(Doctor), id);
            }

            await _unitOfWork.Doctors.DeleteAsync(doctorToDelete);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
