using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Doctor;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Threading.Tasks;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
    /// <summary>
    /// Implements all Doctor use cases.
    ///
    /// Key difference from PatientService:
    /// Doctors have a foreign key to Department.
    /// When creating/updating, we validate the DepartmentId exists before saving.
    /// When reading, we use eager-loading versions of the repository
    /// so DoctorDto.DepartmentName gets populated correctly.
    /// </summary>
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

        /// <inheritdoc />
        public async Task<PagedResponse<DoctorDto>> GetPagedAsync(DoctorQueryParams queryParams)
        {
            // GetPagedAsync in DoctorRepository always includes Department,
            // so DoctorProfile can safely read src.Department.Name
            var (doctors, totalCount) = await _unitOfWork.Doctors.GetPagedAsync(queryParams);

            var doctorDtos = _mapper.Map<System.Collections.Generic.List<DoctorDto>>(doctors);

            return PagedResponse<DoctorDto>.Create(
                doctorDtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        /// <inheritdoc />
        public async Task<DoctorDto> GetDoctorByIdAsync(Guid id)
        {
            // Use the eager-loading version — plain GetByIdAsync won't load Department
            var doctor = await _unitOfWork.Doctors.GetByIdWithDepartmentAsync(id);
            if (doctor == null)
                throw new NotFoundException(nameof(Doctor), id);

            return _mapper.Map<DoctorDto>(doctor);
        }

        /// <inheritdoc />
        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createDoctorDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            // Business rule: the referenced department must exist.
            // A doctor cannot belong to a department that doesn't exist.
            var department = await _unitOfWork.Departments.GetByIdAsync(createDoctorDto.DepartmentId);
            if (department == null)
                throw new NotFoundException(nameof(Department), createDoctorDto.DepartmentId);

            var doctor = _mapper.Map<Doctor>(createDoctorDto);
            await _unitOfWork.Doctors.AddAsync(doctor);
            await _unitOfWork.SaveChangesAsync();

            // After save, reload with Department so the response includes DepartmentName
            // (the entity we just created doesn't have Department loaded yet)
            var created = await _unitOfWork.Doctors.GetByIdWithDepartmentAsync(doctor.Id);
            return _mapper.Map<DoctorDto>(created!);
        }

        /// <inheritdoc />
        public async Task UpdateDoctorAsync(UpdateDoctorDto updateDoctorDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDoctorDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            // Use plain GetById for the update — we only need the entity, not its relations
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(updateDoctorDto.Id);
            if (doctor == null)
                throw new NotFoundException(nameof(Doctor), updateDoctorDto.Id);

            // Only validate the new DepartmentId if it actually changed
            if (doctor.DepartmentId != updateDoctorDto.DepartmentId)
            {
                var department = await _unitOfWork.Departments.GetByIdAsync(updateDoctorDto.DepartmentId);
                if (department == null)
                    throw new NotFoundException(nameof(Department), updateDoctorDto.DepartmentId);
            }

            _mapper.Map(updateDoctorDto, doctor);
            await _unitOfWork.Doctors.UpdateAsync(doctor);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task DeleteDoctorAsync(Guid id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
            if (doctor == null)
                throw new NotFoundException(nameof(Doctor), id);

            await _unitOfWork.Doctors.DeleteAsync(doctor);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
