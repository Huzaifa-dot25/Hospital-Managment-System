using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Laboratory;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Repositories;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
    public class LabOrderService : ILabOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateLabOrderDto> _createValidator;
        private readonly IValidator<CollectSampleDto> _collectValidator;
        private readonly IValidator<RecordLabResultsDto> _resultsValidator;

        public LabOrderService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateLabOrderDto> createValidator,
            IValidator<CollectSampleDto> collectValidator,
            IValidator<RecordLabResultsDto> resultsValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _collectValidator = collectValidator;
            _resultsValidator = resultsValidator;
        }

        public async Task<PagedResponse<LabOrderDto>> GetPagedAsync(LabOrderQueryParams queryParams)
        {
            var (orders, totalCount) = await _unitOfWork.LabOrders.GetPagedAsync(queryParams);
            var dtos = _mapper.Map<List<LabOrderDto>>(orders);

            return PagedResponse<LabOrderDto>.Create(
                dtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        public async Task<LabOrderDto> GetByIdAsync(Guid id)
        {
            var order = await _unitOfWork.LabOrders.GetByIdWithDetailsAsync(id);
            if (order == null)
                throw new NotFoundException(nameof(LabOrder), id);

            return _mapper.Map<LabOrderDto>(order);
        }

        public async Task<LabOrderDto> CreateOrderAsync(CreateLabOrderDto createDto)
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

            var labOrder = _mapper.Map<LabOrder>(createDto);
            labOrder.OrderDate = DateTime.UtcNow;
            labOrder.Status = LabOrderStatus.Ordered;

            foreach (var item in labOrder.Items)
            {
                var test = await _unitOfWork.LabTests.GetByIdAsync(item.LabTestId);
                if (test == null)
                    throw new NotFoundException(nameof(LabTest), item.LabTestId);

                item.Unit = test.Unit;
                item.ReferenceRange = test.ReferenceRange;
            }

            await _unitOfWork.LabOrders.AddAsync(labOrder);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.LabOrders.GetByIdWithDetailsAsync(labOrder.Id);
            return _mapper.Map<LabOrderDto>(created!);
        }

        public async Task<LabOrderDto> CollectSampleAsync(Guid id, CollectSampleDto collectDto)
        {
            var validationResult = await _collectValidator.ValidateAsync(collectDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var order = await _unitOfWork.LabOrders.GetByIdAsync(id);
            if (order == null)
                throw new NotFoundException(nameof(LabOrder), id);

            if (order.Status != LabOrderStatus.Ordered)
                throw new InvalidOperationException($"Cannot collect sample for order in status '{order.Status}'.");

            order.Status = LabOrderStatus.SampleCollected;
            order.SampleCollectionDate = collectDto.SampleCollectionDate ?? DateTime.UtcNow;
            order.SampleCollectedBy = collectDto.SampleCollectedBy;

            await _unitOfWork.LabOrders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.LabOrders.GetByIdWithDetailsAsync(id);
            return _mapper.Map<LabOrderDto>(updated!);
        }

        public async Task<LabOrderDto> RecordResultsAsync(Guid id, RecordLabResultsDto resultsDto, Guid? labTechUserId = null)
        {
            var validationResult = await _resultsValidator.ValidateAsync(resultsDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var order = await _unitOfWork.LabOrders.GetByIdWithItemsAsync(id);
            if (order == null)
                throw new NotFoundException(nameof(LabOrder), id);

            if (order.Status == LabOrderStatus.Completed || order.Status == LabOrderStatus.Cancelled)
                throw new InvalidOperationException($"Cannot enter results for order in status '{order.Status}'.");

            foreach (var res in resultsDto.Results)
            {
                var item = order.Items.FirstOrDefault(i => i.Id == res.LabItemId);
                if (item != null)
                {
                    item.ResultValue = res.ResultValue;
                    item.IsAbnormal = res.IsAbnormal;
                    if (!string.IsNullOrWhiteSpace(res.Unit))
                        item.Unit = res.Unit;
                    if (!string.IsNullOrWhiteSpace(res.ReferenceRange))
                        item.ReferenceRange = res.ReferenceRange;
                    item.Remarks = res.Remarks;
                    item.PerformedDate = DateTime.UtcNow;
                    item.PerformedByLabTechId = labTechUserId;
                }
            }

            var allHaveResults = order.Items.All(i => !string.IsNullOrWhiteSpace(i.ResultValue));
            if (allHaveResults)
            {
                order.Status = LabOrderStatus.Completed;
                order.CompletedDate = DateTime.UtcNow;
            }
            else
            {
                order.Status = LabOrderStatus.InProcess;
            }

            await _unitOfWork.LabOrders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.LabOrders.GetByIdWithDetailsAsync(id);
            return _mapper.Map<LabOrderDto>(updated!);
        }

        public async Task<LabOrderDto> CancelOrderAsync(Guid id)
        {
            var order = await _unitOfWork.LabOrders.GetByIdAsync(id);
            if (order == null)
                throw new NotFoundException(nameof(LabOrder), id);

            if (order.Status == LabOrderStatus.Completed)
                throw new InvalidOperationException("Cannot cancel an already completed lab order.");

            order.Status = LabOrderStatus.Cancelled;
            await _unitOfWork.LabOrders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.LabOrders.GetByIdWithDetailsAsync(id);
            return _mapper.Map<LabOrderDto>(updated!);
        }
    }
}
