using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Billing;
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
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateInvoiceDto> _createValidator;

        public InvoiceService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateInvoiceDto> createValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
        }

        public async Task<PagedResponse<InvoiceDto>> GetPagedAsync(InvoiceQueryParams queryParams)
        {
            var (invoices, totalCount) = await _unitOfWork.Invoices.GetPagedAsync(queryParams);
            var dtos = _mapper.Map<List<InvoiceDto>>(invoices);

            return PagedResponse<InvoiceDto>.Create(
                dtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        public async Task<InvoiceDto> GetByIdAsync(Guid id)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdWithDetailsAsync(id);
            if (invoice == null)
                throw new NotFoundException(nameof(Invoice), id);

            return _mapper.Map<InvoiceDto>(invoice);
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var patient = await _unitOfWork.Patients.GetByIdAsync(createDto.PatientId);
            if (patient == null)
                throw new NotFoundException(nameof(Patient), createDto.PatientId);

            if (createDto.AppointmentId.HasValue)
            {
                var appt = await _unitOfWork.Appointments.GetByIdAsync(createDto.AppointmentId.Value);
                if (appt == null)
                    throw new NotFoundException(nameof(Appointment), createDto.AppointmentId.Value);
            }

            if (createDto.PrescriptionId.HasValue)
            {
                var rx = await _unitOfWork.Prescriptions.GetByIdAsync(createDto.PrescriptionId.Value);
                if (rx == null)
                    throw new NotFoundException(nameof(Prescription), createDto.PrescriptionId.Value);
            }

            if (createDto.LabOrderId.HasValue)
            {
                var lab = await _unitOfWork.LabOrders.GetByIdAsync(createDto.LabOrderId.Value);
                if (lab == null)
                    throw new NotFoundException(nameof(LabOrder), createDto.LabOrderId.Value);
            }

            var now = DateTime.UtcNow;
            var count = await _unitOfWork.Invoices.GetCountForCurrentMonthAsync();
            var invoiceNumber = $"INV-{now:yyyyMM}-{(count + 1):D4}";

            var invoice = _mapper.Map<Invoice>(createDto);
            invoice.InvoiceNumber = invoiceNumber;
            invoice.IssueDate = now;
            invoice.DueDate = createDto.DueDate ?? now.AddDays(30);
            invoice.Status = InvoiceStatus.Pending;
            invoice.PaidAmount = 0m;

            // Compute line items and totals
            decimal subTotal = 0m;
            foreach (var item in invoice.Items)
            {
                item.TotalPrice = Math.Round(item.UnitPrice * item.Quantity, 2);
                subTotal += item.TotalPrice;
            }

            invoice.SubTotal = subTotal;
            invoice.TaxAmount = Math.Round(subTotal * (createDto.TaxPercentage / 100m), 2);
            invoice.TotalAmount = Math.Max(0, Math.Round(subTotal + invoice.TaxAmount - createDto.DiscountAmount, 2));

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Invoices.GetByIdWithDetailsAsync(invoice.Id);
            return _mapper.Map<InvoiceDto>(created!);
        }

        public async Task<InvoiceDto> CancelInvoiceAsync(Guid id)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
            if (invoice == null)
                throw new NotFoundException(nameof(Invoice), id);

            if (invoice.Status == InvoiceStatus.Paid)
                throw new InvalidOperationException("Cannot cancel an invoice that is already fully paid.");

            invoice.Status = InvoiceStatus.Cancelled;
            await _unitOfWork.Invoices.UpdateAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Invoices.GetByIdWithDetailsAsync(id);
            return _mapper.Map<InvoiceDto>(updated!);
        }
    }
}
