using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Pharmacy;
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
    public class MedicationService : IMedicationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateMedicationDto> _createValidator;
        private readonly IValidator<UpdateMedicationDto> _updateValidator;

        public MedicationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateMedicationDto> createValidator,
            IValidator<UpdateMedicationDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<PagedResponse<MedicationDto>> GetPagedAsync(MedicationQueryParams queryParams)
        {
            var (medications, totalCount) = await _unitOfWork.Medications.GetPagedAsync(queryParams);
            var dtos = _mapper.Map<List<MedicationDto>>(medications);

            return PagedResponse<MedicationDto>.Create(
                dtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        public async Task<MedicationDto> GetMedicationByIdAsync(Guid id)
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(id);
            if (medication == null)
                throw new NotFoundException(nameof(Medication), id);

            return _mapper.Map<MedicationDto>(medication);
        }

        public async Task<IReadOnlyList<MedicationDto>> GetLowStockAsync(int threshold)
        {
            var medications = await _unitOfWork.Medications.GetLowStockAsync(threshold);
            return _mapper.Map<List<MedicationDto>>(medications);
        }

        public async Task<MedicationDto> CreateMedicationAsync(CreateMedicationDto createDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var medication = _mapper.Map<Medication>(createDto);
            await _unitOfWork.Medications.AddAsync(medication);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<MedicationDto>(medication);
        }

        public async Task UpdateMedicationAsync(UpdateMedicationDto updateDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var medication = await _unitOfWork.Medications.GetByIdAsync(updateDto.Id);
            if (medication == null)
                throw new NotFoundException(nameof(Medication), updateDto.Id);

            _mapper.Map(updateDto, medication);
            await _unitOfWork.Medications.UpdateAsync(medication);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteMedicationAsync(Guid id)
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(id);
            if (medication == null)
                throw new NotFoundException(nameof(Medication), id);

            await _unitOfWork.Medications.DeleteAsync(medication);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
