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
using System.Threading.Tasks;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<ProcessPaymentDto> _paymentValidator;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<ProcessPaymentDto> paymentValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _paymentValidator = paymentValidator;
        }

        public async Task<PagedResponse<PaymentDto>> GetPagedAsync(PaymentQueryParams queryParams)
        {
            var (payments, totalCount) = await _unitOfWork.Payments.GetPagedAsync(queryParams);
            var dtos = _mapper.Map<List<PaymentDto>>(payments);

            return PagedResponse<PaymentDto>.Create(
                dtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        public async Task<PaymentDto> GetByIdAsync(Guid id)
        {
            var payment = await _unitOfWork.Payments.GetByIdWithDetailsAsync(id);
            if (payment == null)
                throw new NotFoundException(nameof(Payment), id);

            return _mapper.Map<PaymentDto>(payment);
        }

        public async Task<IReadOnlyList<PaymentDto>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
            if (invoice == null)
                throw new NotFoundException(nameof(Invoice), invoiceId);

            var payments = await _unitOfWork.Payments.GetByInvoiceIdAsync(invoiceId);
            return _mapper.Map<List<PaymentDto>>(payments);
        }

        public async Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto paymentDto, Guid? cashierUserId = null)
        {
            var validationResult = await _paymentValidator.ValidateAsync(paymentDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var invoice = await _unitOfWork.Invoices.GetByIdWithPaymentsAsync(paymentDto.InvoiceId);
            if (invoice == null)
                throw new NotFoundException(nameof(Invoice), paymentDto.InvoiceId);

            if (invoice.Status == InvoiceStatus.Cancelled)
                throw new InvalidOperationException("Cannot record payment for a cancelled invoice.");

            if (invoice.Status == InvoiceStatus.Paid)
                throw new InvalidOperationException("Invoice is already fully paid.");

            var remainingBalance = invoice.TotalAmount - invoice.PaidAmount;
            if (paymentDto.Amount > remainingBalance)
                throw new InvalidOperationException(
                    $"Payment amount ({paymentDto.Amount:F2}) exceeds outstanding balance ({remainingBalance:F2}).");

            var payment = _mapper.Map<Payment>(paymentDto);
            payment.PaymentDate = DateTime.UtcNow;
            payment.Status = PaymentStatus.Success;
            payment.ReceivedByUserId = cashierUserId;

            if (string.IsNullOrWhiteSpace(payment.TransactionReference))
                payment.TransactionReference = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant();

            invoice.PaidAmount += paymentDto.Amount;
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;

            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.Invoices.UpdateAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PaymentDto>(payment);
        }
    }
}
