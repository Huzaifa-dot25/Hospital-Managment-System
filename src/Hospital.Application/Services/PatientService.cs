using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Patient;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
    public class PatientService : IPatientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreatePatientDto> _createValidator;
        private readonly IValidator<UpdatePatientDto> _updateValidator;

        public PatientService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreatePatientDto> createValidator,
            IValidator<UpdatePatientDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<PatientDto>> GetAllPatientsAsync()
        {
            var patients = await _unitOfWork.Patients.GetAllAsync();
            return _mapper.Map<IEnumerable<PatientDto>>(patients);
        }

        public async Task<PatientDto> GetPatientByIdAsync(Guid id)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            if (patient == null)
            {
                throw new NotFoundException(nameof(Patient), id);
            }
            return _mapper.Map<PatientDto>(patient);
        }

        public async Task<PatientDto> CreatePatientAsync(CreatePatientDto createPatientDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createPatientDto);
            if (!validationResult.IsValid)
            {
                throw new AppValidationException(validationResult.Errors);
            }

            var patient = _mapper.Map<Patient>(createPatientDto);
            patient.CreatedDate = DateTime.UtcNow;

            await _unitOfWork.Patients.AddAsync(patient);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PatientDto>(patient);
        }

        public async Task UpdatePatientAsync(UpdatePatientDto updatePatientDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updatePatientDto);
            if (!validationResult.IsValid)
            {
                throw new AppValidationException(validationResult.Errors);
            }

            var patientToUpdate = await _unitOfWork.Patients.GetByIdAsync(updatePatientDto.Id);
            if (patientToUpdate == null)
            {
                throw new NotFoundException(nameof(Patient), updatePatientDto.Id);
            }

            _mapper.Map(updatePatientDto, patientToUpdate);
            patientToUpdate.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Patients.UpdateAsync(patientToUpdate);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeletePatientAsync(Guid id)
        {
            var patientToDelete = await _unitOfWork.Patients.GetByIdAsync(id);
            if (patientToDelete == null)
            {
                throw new NotFoundException(nameof(Patient), id);
            }

            await _unitOfWork.Patients.DeleteAsync(patientToDelete);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
