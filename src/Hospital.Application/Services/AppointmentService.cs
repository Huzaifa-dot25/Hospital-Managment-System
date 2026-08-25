using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Appointment;
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
    /// Implements all Appointment use cases.
    ///
    /// Appointments are the core of a hospital management system.
    /// Every appointment links one Patient to one Doctor at a specific date/time.
    ///
    /// Business rules enforced here:
    ///   - Both PatientId and DoctorId must reference existing records
    ///   - Appointment date must be in the future (validated by FluentValidation)
    ///   - Updating loads the plain entity (no joins needed for update)
    ///   - Reading always loads Patient and Doctor (needed for name display)
    /// </summary>
    public class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateAppointmentDto> _createValidator;
        private readonly IValidator<UpdateAppointmentDto> _updateValidator;

        public AppointmentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateAppointmentDto> createValidator,
            IValidator<UpdateAppointmentDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        /// <inheritdoc />
        public async Task<PagedResponse<AppointmentDto>> GetPagedAsync(AppointmentQueryParams queryParams)
        {
            // GetPagedAsync in AppointmentRepository always includes Patient + Doctor
            var (appointments, totalCount) = await _unitOfWork.Appointments.GetPagedAsync(queryParams);

            var dtos = _mapper.Map<System.Collections.Generic.List<AppointmentDto>>(appointments);

            return PagedResponse<AppointmentDto>.Create(
                dtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        /// <inheritdoc />
        public async Task<AppointmentDto> GetAppointmentByIdAsync(Guid id)
        {
            // Use the eager-loading version to get Patient + Doctor names
            var appointment = await _unitOfWork.Appointments.GetByIdWithDetailsAsync(id);
            if (appointment == null)
                throw new NotFoundException(nameof(Appointment), id);

            return _mapper.Map<AppointmentDto>(appointment);
        }

        /// <inheritdoc />
        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createAppointmentDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            // Verify both Patient and Doctor exist before creating the appointment.
            // Without this check, EF would throw a foreign key constraint violation —
            // which is a database error, not a user-friendly 404.
            var patient = await _unitOfWork.Patients.GetByIdAsync(createAppointmentDto.PatientId);
            if (patient == null)
                throw new NotFoundException(nameof(Patient), createAppointmentDto.PatientId);

            var doctor = await _unitOfWork.Doctors.GetByIdAsync(createAppointmentDto.DoctorId);
            if (doctor == null)
                throw new NotFoundException(nameof(Doctor), createAppointmentDto.DoctorId);

            var appointment = _mapper.Map<Appointment>(createAppointmentDto);
            await _unitOfWork.Appointments.AddAsync(appointment);
            await _unitOfWork.SaveChangesAsync();

            // Reload with Patient + Doctor loaded so the response DTO is fully populated
            var created = await _unitOfWork.Appointments.GetByIdWithDetailsAsync(appointment.Id);
            return _mapper.Map<AppointmentDto>(created!);
        }

        /// <inheritdoc />
        public async Task UpdateAppointmentAsync(UpdateAppointmentDto updateAppointmentDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateAppointmentDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var appointment = await _unitOfWork.Appointments.GetByIdAsync(updateAppointmentDto.Id);
            if (appointment == null)
                throw new NotFoundException(nameof(Appointment), updateAppointmentDto.Id);

            _mapper.Map(updateAppointmentDto, appointment);
            await _unitOfWork.Appointments.UpdateAsync(appointment);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task DeleteAppointmentAsync(Guid id)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null)
                throw new NotFoundException(nameof(Appointment), id);

            await _unitOfWork.Appointments.DeleteAsync(appointment);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
