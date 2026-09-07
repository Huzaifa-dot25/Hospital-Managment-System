using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Pharmacy;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Repositories;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreatePrescriptionDto> _createValidator;

        public PrescriptionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreatePrescriptionDto> createValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
        }

        public async Task<PagedResponse<PrescriptionDto>> GetPagedAsync(PrescriptionQueryParams queryParams)
        {
            var (prescriptions, totalCount) = await _unitOfWork.Prescriptions.GetPagedAsync(queryParams);
            var dtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);

            return PagedResponse<PrescriptionDto>.Create(
                dtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        public async Task<PrescriptionDto> GetPrescriptionByIdAsync(Guid id)
        {
            var prescription = await _unitOfWork.Prescriptions.GetByIdWithDetailsAsync(id);
            if (prescription == null)
                throw new NotFoundException(nameof(Prescription), id);

            return _mapper.Map<PrescriptionDto>(prescription);
        }

        public async Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionDto createDto)
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

            if (createDto.MedicalRecordId.HasValue)
            {
                var record = await _unitOfWork.MedicalRecords.GetByIdAsync(createDto.MedicalRecordId.Value);
                if (record == null)
                    throw new NotFoundException(nameof(MedicalRecord), createDto.MedicalRecordId.Value);
            }

            foreach (var item in createDto.Items)
            {
                var medication = await _unitOfWork.Medications.GetByIdAsync(item.MedicationId);
                if (medication == null)
                    throw new NotFoundException(nameof(Medication), item.MedicationId);
            }

            var prescription = _mapper.Map<Prescription>(createDto);
            prescription.Status = PrescriptionStatus.Pending;

            await _unitOfWork.Prescriptions.AddAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Prescriptions.GetByIdWithDetailsAsync(prescription.Id);
            return _mapper.Map<PrescriptionDto>(created!);
        }

        public async Task<PrescriptionDto> DispensePrescriptionAsync(Guid id, DispensePrescriptionDto dispenseDto)
        {
            var prescription = await _unitOfWork.Prescriptions.GetByIdWithItemsAsync(id);
            if (prescription == null)
                throw new NotFoundException(nameof(Prescription), id);

            if (prescription.Status != PrescriptionStatus.Pending)
                throw new InvalidOperationException(
                    $"Prescription cannot be dispensed because its current status is '{prescription.Status}'.");

            // Verify stock for all items before making any changes
            foreach (var item in prescription.Items)
            {
                var medication = await _unitOfWork.Medications.GetByIdAsync(item.MedicationId);
                if (medication == null)
                    throw new NotFoundException(nameof(Medication), item.MedicationId);

                if (medication.StockQuantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for medication '{medication.Name}'. Required: {item.Quantity}, Available: {medication.StockQuantity}.");

                medication.StockQuantity -= item.Quantity;
                await _unitOfWork.Medications.UpdateAsync(medication);
            }

            prescription.Status = PrescriptionStatus.Dispensed;
            prescription.DispensedDate = DateTime.UtcNow;
            prescription.DispensedByPharmacistId = dispenseDto.PharmacistId;

            if (!string.IsNullOrWhiteSpace(dispenseDto.DispensingNotes))
            {
                prescription.Notes = string.IsNullOrWhiteSpace(prescription.Notes)
                    ? $"[Dispensed]: {dispenseDto.DispensingNotes}"
                    : $"{prescription.Notes} | [Dispensed]: {dispenseDto.DispensingNotes}";
            }

            await _unitOfWork.Prescriptions.UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            var result = await _unitOfWork.Prescriptions.GetByIdWithDetailsAsync(prescription.Id);
            return _mapper.Map<PrescriptionDto>(result ?? prescription);
        }

        public async Task CancelPrescriptionAsync(Guid id)
        {
            var prescription = await _unitOfWork.Prescriptions.GetByIdAsync(id);
            if (prescription == null)
                throw new NotFoundException(nameof(Prescription), id);

            if (prescription.Status == PrescriptionStatus.Dispensed)
                throw new InvalidOperationException("Cannot cancel an already dispensed prescription.");

            prescription.Status = PrescriptionStatus.Cancelled;
            await _unitOfWork.Prescriptions.UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
