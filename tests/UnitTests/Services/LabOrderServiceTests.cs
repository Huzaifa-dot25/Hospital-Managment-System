using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Hospital.Application.DTOs.Laboratory;
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
    public class LabOrderServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILabOrderRepository> _mockLabOrderRepository;
        private readonly Mock<ILabTestRepository> _mockLabTestRepository;
        private readonly Mock<IPatientRepository> _mockPatientRepository;
        private readonly Mock<IDoctorRepository> _mockDoctorRepository;
        private readonly Mock<IMedicalRecordRepository> _mockMedicalRecordRepository;
        private readonly IMapper _mapper;
        private readonly LabOrderService _sut;

        public LabOrderServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLabOrderRepository = new Mock<ILabOrderRepository>();
            _mockLabTestRepository = new Mock<ILabTestRepository>();
            _mockPatientRepository = new Mock<IPatientRepository>();
            _mockDoctorRepository = new Mock<IDoctorRepository>();
            _mockMedicalRecordRepository = new Mock<IMedicalRecordRepository>();

            _mockUnitOfWork.Setup(u => u.LabOrders).Returns(_mockLabOrderRepository.Object);
            _mockUnitOfWork.Setup(u => u.LabTests).Returns(_mockLabTestRepository.Object);
            _mockUnitOfWork.Setup(u => u.Patients).Returns(_mockPatientRepository.Object);
            _mockUnitOfWork.Setup(u => u.Doctors).Returns(_mockDoctorRepository.Object);
            _mockUnitOfWork.Setup(u => u.MedicalRecords).Returns(_mockMedicalRecordRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<LaboratoryProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<CreateLabOrderDto> createValidator = new CreateLabOrderDtoValidator();
            IValidator<CollectSampleDto> collectValidator = new CollectSampleDtoValidator();
            IValidator<RecordLabResultsDto> resultsValidator = new RecordLabResultsDtoValidator();

            _sut = new LabOrderService(
                _mockUnitOfWork.Object,
                _mapper,
                createValidator,
                collectValidator,
                resultsValidator);
        }

        [Fact]
        public async Task GetPagedAsync_WhenOrdersExist_ReturnsPagedResponse()
        {
            // ARRANGE
            var order = TestDataBuilder.CreateLabOrder();
            var queryParams = new LabOrderQueryParams { PageNumber = 1, PageSize = 10 };

            _mockLabOrderRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((new List<LabOrder> { order }, 1));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();
        }

        [Fact]
        public async Task GetByIdAsync_WhenOrderExists_ReturnsDto()
        {
            // ARRANGE
            var order = TestDataBuilder.CreateLabOrder();
            _mockLabOrderRepository
                .Setup(r => r.GetByIdWithDetailsAsync(order.Id))
                .ReturnsAsync(order);

            // ACT
            var result = await _sut.GetByIdAsync(order.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Id.Should().Be(order.Id);
            result.Status.Should().Be(LabOrderStatus.Ordered.ToString());
        }

        [Fact]
        public async Task GetByIdAsync_WhenOrderDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockLabOrderRepository
                .Setup(r => r.GetByIdWithDetailsAsync(id))
                .ReturnsAsync((LabOrder?)null);

            // ACT
            Func<Task> act = async () => await _sut.GetByIdAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateOrderAsync_WithValidData_ReturnsCreatedDto()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var doctor = TestDataBuilder.CreateDoctor();
            var test = TestDataBuilder.CreateLabTest();
            var dto = TestDataBuilder.CreateLabOrderDto(patient.Id, doctor.Id, test.Id);

            _mockPatientRepository.Setup(r => r.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
            _mockDoctorRepository.Setup(r => r.GetByIdAsync(doctor.Id)).ReturnsAsync(doctor);
            _mockLabTestRepository.Setup(r => r.GetByIdAsync(test.Id)).ReturnsAsync(test);

            _mockLabOrderRepository
                .Setup(r => r.AddAsync(It.IsAny<LabOrder>()))
                .ReturnsAsync((LabOrder o) => o);

            var createdOrder = TestDataBuilder.CreateLabOrder(null, patient.Id, doctor.Id, test);
            _mockLabOrderRepository
                .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(createdOrder);

            // ACT
            var result = await _sut.CreateOrderAsync(dto);

            // ASSERT
            result.Should().NotBeNull();
            _mockLabOrderRepository.Verify(r => r.AddAsync(It.IsAny<LabOrder>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateOrderAsync_WhenPatientNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var dto = TestDataBuilder.CreateLabOrderDto();
            _mockPatientRepository.Setup(r => r.GetByIdAsync(dto.PatientId)).ReturnsAsync((Patient?)null);

            // ACT
            Func<Task> act = async () => await _sut.CreateOrderAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateOrderAsync_WhenDoctorNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var dto = TestDataBuilder.CreateLabOrderDto(patient.Id);

            _mockPatientRepository.Setup(r => r.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
            _mockDoctorRepository.Setup(r => r.GetByIdAsync(dto.DoctorId)).ReturnsAsync((Doctor?)null);

            // ACT
            Func<Task> act = async () => await _sut.CreateOrderAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CollectSampleAsync_WhenStatusIsOrdered_TransitionsToSampleCollected()
        {
            // ARRANGE
            var order = TestDataBuilder.CreateLabOrder();
            order.Status = LabOrderStatus.Ordered;

            _mockLabOrderRepository.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
            _mockLabOrderRepository.Setup(r => r.GetByIdWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var collectDto = new CollectSampleDto
            {
                SampleCollectedBy = "Nurse Sarah",
                SampleCollectionDate = DateTime.UtcNow
            };

            // ACT
            var result = await _sut.CollectSampleAsync(order.Id, collectDto);

            // ASSERT
            order.Status.Should().Be(LabOrderStatus.SampleCollected);
            order.SampleCollectedBy.Should().Be("Nurse Sarah");
            _mockLabOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CollectSampleAsync_WhenStatusNotOrdered_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var order = TestDataBuilder.CreateLabOrder();
            order.Status = LabOrderStatus.Completed;

            _mockLabOrderRepository.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

            var collectDto = new CollectSampleDto { SampleCollectedBy = "Nurse Sarah" };

            // ACT
            Func<Task> act = async () => await _sut.CollectSampleAsync(order.Id, collectDto);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cannot collect sample*");
        }

        [Fact]
        public async Task RecordResultsAsync_WhenAllResultsEntered_TransitionsToCompleted()
        {
            // ARRANGE
            var order = TestDataBuilder.CreateLabOrder();
            order.Status = LabOrderStatus.SampleCollected;
            var item = new List<LabOrderItem>(order.Items)[0];

            _mockLabOrderRepository.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
            _mockLabOrderRepository.Setup(r => r.GetByIdWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var resultsDto = new RecordLabResultsDto
            {
                Results = new List<RecordLabItemResultDto>
                {
                    new()
                    {
                        LabItemId = item.Id,
                        ResultValue = "7.5",
                        IsAbnormal = false,
                        Remarks = "Within normal limits"
                    }
                }
            };

            // ACT
            var result = await _sut.RecordResultsAsync(order.Id, resultsDto);

            // ASSERT
            order.Status.Should().Be(LabOrderStatus.Completed);
            order.CompletedDate.Should().NotBeNull();
            item.ResultValue.Should().Be("7.5");
            _mockLabOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RecordResultsAsync_WhenOrderAlreadyCompleted_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var order = TestDataBuilder.CreateLabOrder();
            order.Status = LabOrderStatus.Completed;

            _mockLabOrderRepository.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

            var resultsDto = new RecordLabResultsDto
            {
                Results = new List<RecordLabItemResultDto>
                {
                    new()
                    {
                        LabItemId = Guid.NewGuid(),
                        ResultValue = "7.5"
                    }
                }
            };

            // ACT
            Func<Task> act = async () => await _sut.RecordResultsAsync(order.Id, resultsDto);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cannot enter results*");
        }

        [Fact]
        public async Task CancelOrderAsync_WhenPending_TransitionsToCancelled()
        {
            // ARRANGE
            var order = TestDataBuilder.CreateLabOrder();
            order.Status = LabOrderStatus.Ordered;

            _mockLabOrderRepository.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
            _mockLabOrderRepository.Setup(r => r.GetByIdWithDetailsAsync(order.Id)).ReturnsAsync(order);

            // ACT
            var result = await _sut.CancelOrderAsync(order.Id);

            // ASSERT
            order.Status.Should().Be(LabOrderStatus.Cancelled);
            _mockLabOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CancelOrderAsync_WhenAlreadyCompleted_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var order = TestDataBuilder.CreateLabOrder();
            order.Status = LabOrderStatus.Completed;

            _mockLabOrderRepository.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

            // ACT
            Func<Task> act = async () => await _sut.CancelOrderAsync(order.Id);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cannot cancel an already completed*");
        }
    }
}
