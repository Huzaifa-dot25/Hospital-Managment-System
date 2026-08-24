using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Appointment;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
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

        public async Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync()
        {
            // Use GetAllWithDetailsAsync() to load Patient and Doctor navigation properties.
            // AppointmentProfile maps PatientName = Patient.FirstName + LastName
            // and DoctorName = Doctor.FirstName + LastName.
            // Without Include(), those would be null and mapping would throw.
            var appointments = await _unitOfWork.Appointments.GetAllWithDetailsAsync();
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<AppointmentDto> GetAppointmentByIdAsync(Guid id)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdWithDetailsAsync(id);
            if (appointment == null)
            {
                throw new NotFoundException(nameof(Appointment), id);
            }
            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createAppointmentDto);
            if (!validationResult.IsValid)
            {
                throw new AppValidationException(validationResult.Errors);
            }

            var patient = await _unitOfWork.Patients.GetByIdAsync(createAppointmentDto.PatientId);
            if (patient == null)
            {
                throw new NotFoundException(nameof(Patient), createAppointmentDto.PatientId);
            }

            var doctor = await _unitOfWork.Doctors.GetByIdAsync(createAppointmentDto.DoctorId);
            if (doctor == null)
            {
                throw new NotFoundException(nameof(Doctor), createAppointmentDto.DoctorId);
            }

            var appointment = _mapper.Map<Appointment>(createAppointmentDto);
            appointment.CreatedDate = DateTime.UtcNow;

            await _unitOfWork.Appointments.AddAsync(appointment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task UpdateAppointmentAsync(UpdateAppointmentDto updateAppointmentDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateAppointmentDto);
            if (!validationResult.IsValid)
            {
                throw new AppValidationException(validationResult.Errors);
            }

            var appointmentToUpdate = await _unitOfWork.Appointments.GetByIdAsync(updateAppointmentDto.Id);
            if (appointmentToUpdate == null)
            {
                throw new NotFoundException(nameof(Appointment), updateAppointmentDto.Id);
            }

            _mapper.Map(updateAppointmentDto, appointmentToUpdate);
            appointmentToUpdate.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Appointments.UpdateAsync(appointmentToUpdate);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAppointmentAsync(Guid id)
        {
            var appointmentToDelete = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointmentToDelete == null)
            {
                throw new NotFoundException(nameof(Appointment), id);
            }

            await _unitOfWork.Appointments.DeleteAsync(appointmentToDelete);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
