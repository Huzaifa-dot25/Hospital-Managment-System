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
    public class InvoiceServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
        private readonly Mock<IPatientRepository> _mockPatientRepository;
        private readonly IMapper _mapper;
        private readonly InvoiceService _sut;

        public InvoiceServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockInvoiceRepository = new Mock<IInvoiceRepository>();
            _mockPatientRepository = new Mock<IPatientRepository>();

            _mockUnitOfWork.Setup(u => u.Invoices).Returns(_mockInvoiceRepository.Object);
            _mockUnitOfWork.Setup(u => u.Patients).Returns(_mockPatientRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<BillingProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<CreateInvoiceDto> createValidator = new CreateInvoiceDtoValidator();

            _sut = new InvoiceService(
                _mockUnitOfWork.Object,
                _mapper,
                createValidator);
        }

        [Fact]
        public async Task GetPagedAsync_WhenInvoicesExist_ReturnsPagedResponse()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice();
            var queryParams = new InvoiceQueryParams { PageNumber = 1, PageSize = 10 };

            _mockInvoiceRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((new List<Invoice> { invoice }, 1));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();
        }

        [Fact]
        public async Task GetByIdAsync_WhenInvoiceExists_ReturnsDto()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice();
            _mockInvoiceRepository
                .Setup(r => r.GetByIdWithDetailsAsync(invoice.Id))
                .ReturnsAsync(invoice);

            // ACT
            var result = await _sut.GetByIdAsync(invoice.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Id.Should().Be(invoice.Id);
            result.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
            result.TotalAmount.Should().Be(invoice.TotalAmount);
        }

        [Fact]
        public async Task GetByIdAsync_WhenInvoiceDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockInvoiceRepository
                .Setup(r => r.GetByIdWithDetailsAsync(id))
                .ReturnsAsync((Invoice?)null);

            // ACT
            Func<Task> act = async () => await _sut.GetByIdAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateInvoiceAsync_WithValidData_CalculatesTotalsAndSaves()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var dto = TestDataBuilder.CreateInvoiceDto(patient.Id, unitPrice: 200m, quantity: 2, taxPercentage: 10m, discount: 40m);
            // Expected: SubTotal = 400, Tax = 40, Total = 400 + 40 - 40 = 400

            _mockPatientRepository.Setup(r => r.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
            _mockInvoiceRepository.Setup(r => r.GetCountForCurrentMonthAsync()).ReturnsAsync(5);
            _mockInvoiceRepository
                .Setup(r => r.AddAsync(It.IsAny<Invoice>()))
                .ReturnsAsync((Invoice i) => i);

            var createdInvoice = TestDataBuilder.CreateInvoice(null, patient.Id, 200m, 2, 10m, 40m);
            _mockInvoiceRepository
                .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(createdInvoice);

            // ACT
            var result = await _sut.CreateInvoiceAsync(dto);

            // ASSERT
            result.Should().NotBeNull();
            _mockInvoiceRepository.Verify(r => r.AddAsync(It.Is<Invoice>(i =>
                i.SubTotal == 400m &&
                i.TaxAmount == 40m &&
                i.TotalAmount == 400m &&
                i.Status == InvoiceStatus.Pending
            )), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateInvoiceAsync_WhenPatientNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var dto = TestDataBuilder.CreateInvoiceDto();
            _mockPatientRepository.Setup(r => r.GetByIdAsync(dto.PatientId)).ReturnsAsync((Patient?)null);

            // ACT
            Func<Task> act = async () => await _sut.CreateInvoiceAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateInvoiceAsync_WithInvalidData_ThrowsValidationException()
        {
            // ARRANGE
            var dto = new CreateInvoiceDto(); // Missing PatientId, Items empty

            // ACT
            Func<Task> act = async () => await _sut.CreateInvoiceAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        [Fact]
        public async Task CancelInvoiceAsync_WhenPending_TransitionsToCancelled()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice();
            invoice.Status = InvoiceStatus.Pending;

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
            _mockInvoiceRepository.Setup(r => r.GetByIdWithDetailsAsync(invoice.Id)).ReturnsAsync(invoice);

            // ACT
            var result = await _sut.CancelInvoiceAsync(invoice.Id);

            // ASSERT
            invoice.Status.Should().Be(InvoiceStatus.Cancelled);
            _mockInvoiceRepository.Verify(r => r.UpdateAsync(invoice), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CancelInvoiceAsync_WhenPaid_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var invoice = TestDataBuilder.CreateInvoice();
            invoice.Status = InvoiceStatus.Paid;

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);

            // ACT
            Func<Task> act = async () => await _sut.CancelInvoiceAsync(invoice.Id);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already fully paid*");
        }
    }
}
