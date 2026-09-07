using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Hospital.Application.DTOs.Billing;
using Hospital.Application.Exceptions;
using Hospital.Application.Mappings;
using Hospital.Application.Services;
using Hospital.Application.Validations;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Repositories;
using Hospital.Shared.Queries;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnitTests.Helpers;
using Xunit;

namespace UnitTests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IPaymentRepository> _mockPaymentRepository;
        private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
        private readonly IMapper _mapper;
        private readonly PaymentService _sut;

        public PaymentServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockPaymentRepository = new Mock<IPaymentRepository>();
            _mockInvoiceRepository = new Mock<IInvoiceRepository>();

            _mockUnitOfWork.Setup(u => u.Payments).Returns(_mockPaymentRepository.Object);
            _mockUnitOfWork.Setup(u => u.Invoices).Returns(_mockInvoiceRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<BillingProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<ProcessPaymentDto> paymentValidator = new ProcessPaymentDtoValidator();

            _sut = new PaymentService(
                _mockUnitOfWork.Object,
                _mapper,
                paymentValidator);
        }

        [Fact]
        public async Task GetPagedAsync_WhenPaymentsExist_ReturnsPagedResponse()
        {
            // ARRANGE
            var payment = TestDataBuilder.CreatePayment();
            var queryParams = new PaymentQueryParams { PageNumber = 1, PageSize = 10 };

            _mockPaymentRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((new List<Payment> { payment }, 1));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();
        }

        [Fact]
        public async Task GetByIdAsync_WhenExists_ReturnsDto()
        {
            // ARRANGE
            var payment = TestDataBuilder.CreatePayment();
            _mockPaymentRepository
                .Setup(r => r.GetByIdWithDetailsAsync(payment.Id))
                .ReturnsAsync(payment);

            // ACT
            var result = await _sut.GetByIdAsync(payment.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Id.Should().Be(payment.Id);
            result.Amount.Should().Be(payment.Amount);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockPaymentRepository
                .Setup(r => r.GetByIdWithDetailsAsync(id))
                .ReturnsAsync((Payment?)null);

            // ACT
            Func<Task> act = async () => await _sut.GetByIdAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetByInvoiceIdAsync_WhenInvoiceExists_ReturnsPayments()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice();
            var payment = TestDataBuilder.CreatePayment(null, invoice.Id);

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
            _mockPaymentRepository.Setup(r => r.GetByInvoiceIdAsync(invoice.Id)).ReturnsAsync(new List<Payment> { payment });

            // ACT
            var result = await _sut.GetByInvoiceIdAsync(invoice.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Should().ContainSingle();
        }

        [Fact]
        public async Task ProcessPaymentAsync_PartialPayment_UpdatesPaidAmountAndSetsPartiallyPaid()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice(null, null, unitPrice: 200m, quantity: 1, taxPercentage: 0m);
            // TotalAmount = 200, PaidAmount = 0
            invoice.Status = InvoiceStatus.Pending;

            _mockInvoiceRepository.Setup(r => r.GetByIdWithPaymentsAsync(invoice.Id)).ReturnsAsync(invoice);
            _mockPaymentRepository
                .Setup(r => r.AddAsync(It.IsAny<Payment>()))
                .ReturnsAsync((Payment p) => p);

            var paymentDto = new ProcessPaymentDto
            {
                InvoiceId = invoice.Id,
                Amount = 100m,
                Method = PaymentMethod.CreditCard,
                Notes = "First installment"
            };

            // ACT
            var result = await _sut.ProcessPaymentAsync(paymentDto);

            // ASSERT
            result.Should().NotBeNull();
            result.Amount.Should().Be(100m);
            invoice.PaidAmount.Should().Be(100m);
            invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);

            _mockPaymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
            _mockInvoiceRepository.Verify(r => r.UpdateAsync(invoice), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessPaymentAsync_FullPayment_UpdatesPaidAmountAndSetsPaid()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice(null, null, unitPrice: 150m, quantity: 1, taxPercentage: 0m);
            invoice.Status = InvoiceStatus.Pending;

            _mockInvoiceRepository.Setup(r => r.GetByIdWithPaymentsAsync(invoice.Id)).ReturnsAsync(invoice);
            _mockPaymentRepository
                .Setup(r => r.AddAsync(It.IsAny<Payment>()))
                .ReturnsAsync((Payment p) => p);

            var paymentDto = new ProcessPaymentDto
            {
                InvoiceId = invoice.Id,
                Amount = 150m,
                Method = PaymentMethod.Cash
            };

            // ACT
            var result = await _sut.ProcessPaymentAsync(paymentDto);

            // ASSERT
            result.Should().NotBeNull();
            invoice.PaidAmount.Should().Be(150m);
            invoice.Status.Should().Be(InvoiceStatus.Paid);

            _mockPaymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
            _mockInvoiceRepository.Verify(r => r.UpdateAsync(invoice), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenInvoiceCancelled_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice();
            invoice.Status = InvoiceStatus.Cancelled;

            _mockInvoiceRepository.Setup(r => r.GetByIdWithPaymentsAsync(invoice.Id)).ReturnsAsync(invoice);

            var paymentDto = new ProcessPaymentDto
            {
                InvoiceId = invoice.Id,
                Amount = 50m
            };

            // ACT
            Func<Task> act = async () => await _sut.ProcessPaymentAsync(paymentDto);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*cancelled invoice*");
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenInvoiceAlreadyPaid_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice(null, null, 100m, 1, 0m);
            invoice.PaidAmount = 100m;
            invoice.Status = InvoiceStatus.Paid;

            _mockInvoiceRepository.Setup(r => r.GetByIdWithPaymentsAsync(invoice.Id)).ReturnsAsync(invoice);

            var paymentDto = new ProcessPaymentDto
            {
                InvoiceId = invoice.Id,
                Amount = 10m
            };

            // ACT
            Func<Task> act = async () => await _sut.ProcessPaymentAsync(paymentDto);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already fully paid*");
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenAmountExceedsBalance_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice(null, null, 100m, 1, 0m);
            invoice.PaidAmount = 50m; // Remaining: 50
            invoice.Status = InvoiceStatus.PartiallyPaid;

            _mockInvoiceRepository.Setup(r => r.GetByIdWithPaymentsAsync(invoice.Id)).ReturnsAsync(invoice);

            var paymentDto = new ProcessPaymentDto
            {
                InvoiceId = invoice.Id,
                Amount = 60m // Exceeds 50
            };

            // ACT
            Func<Task> act = async () => await _sut.ProcessPaymentAsync(paymentDto);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*exceeds outstanding balance*");
        }
    }
}
