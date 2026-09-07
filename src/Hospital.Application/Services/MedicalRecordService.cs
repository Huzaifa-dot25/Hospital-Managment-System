using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.MedicalRecord;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
    public class MedicalRecordService : IMedicalRecordService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateMedicalRecordDto> _createValidator;
        private readonly IValidator<UpdateMedicalRecordDto> _updateValidator;

        public MedicalRecordService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateMedicalRecordDto> createValidator,
            IValidator<UpdateMedicalRecordDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<PagedResponse<MedicalRecordDto>> GetPagedAsync(MedicalRecordQueryParams queryParams)
        {
            var (records, totalCount) = await _unitOfWork.MedicalRecords.GetPagedAsync(queryParams);
            var dtos = _mapper.Map<List<MedicalRecordDto>>(records);

            return PagedResponse<MedicalRecordDto>.Create(
                dtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        public async Task<MedicalRecordDto> GetMedicalRecordByIdAsync(Guid id)
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdWithDetailsAsync(id);
            if (record == null)
                throw new NotFoundException(nameof(MedicalRecord), id);

            return _mapper.Map<MedicalRecordDto>(record);
        }

        public async Task<MedicalRecordDto> CreateMedicalRecordAsync(CreateMedicalRecordDto createDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var patient = await _unitOfWork.Patients.GetByIdAsync(createDto.PatientId);
            if (patient == null)
                throw new NotFoundException(nameof(Patient), createDto.PatientId);

            var doctor = await _unitOfWork.Doctors.GetByIdAsync(createDto.DoctorId);
            if (doctor == null)
                throw new NotFoundException(nameof(Doctor), createDto.DoctorId);

            if (createDto.AppointmentId.HasValue)
            {
                var appointment = await _unitOfWork.Appointments.GetByIdAsync(createDto.AppointmentId.Value);
                if (appointment == null)
                    throw new NotFoundException(nameof(Appointment), createDto.AppointmentId.Value);
            }

            var record = _mapper.Map<MedicalRecord>(createDto);
            await _unitOfWork.MedicalRecords.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.MedicalRecords.GetByIdWithDetailsAsync(record.Id);
            return _mapper.Map<MedicalRecordDto>(created!);
        }

        public async Task UpdateMedicalRecordAsync(UpdateMedicalRecordDto updateDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var record = await _unitOfWork.MedicalRecords.GetByIdAsync(updateDto.Id);
            if (record == null)
                throw new NotFoundException(nameof(MedicalRecord), updateDto.Id);

            _mapper.Map(updateDto, record);
            await _unitOfWork.MedicalRecords.UpdateAsync(record);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteMedicalRecordAsync(Guid id)
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdAsync(id);
            if (record == null)
                throw new NotFoundException(nameof(MedicalRecord), id);

            await _unitOfWork.MedicalRecords.DeleteAsync(record);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
