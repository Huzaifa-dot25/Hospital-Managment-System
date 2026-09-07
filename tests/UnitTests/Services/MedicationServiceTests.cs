using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Pharmacy;
using Hospital.Application.Exceptions;
using Hospital.Application.Mappings;
using Hospital.Application.Services;
using Hospital.Application.Validations;
using Hospital.Domain.Entities;
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
using FluentAssertions;

namespace UnitTests.Services
{
    public class MedicationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMedicationRepository> _mockMedicationRepository;
        private readonly IMapper _mapper;
        private readonly MedicationService _sut;

        public MedicationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMedicationRepository = new Mock<IMedicationRepository>();

            _mockUnitOfWork.Setup(u => u.Medications).Returns(_mockMedicationRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<PharmacyProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<CreateMedicationDto> createValidator = new CreateMedicationDtoValidator();
            IValidator<UpdateMedicationDto> updateValidator = new UpdateMedicationDtoValidator();

            _sut = new MedicationService(
                _mockUnitOfWork.Object,
                _mapper,
                createValidator,
                updateValidator);
        }

        [Fact]
        public async Task GetPagedAsync_WhenMedicationsExist_ReturnsPagedResponse()
        {
            // ARRANGE
            var med = TestDataBuilder.CreateMedication();
            var list = new List<Medication> { med };
            var queryParams = new MedicationQueryParams { PageNumber = 1, PageSize = 10 };

            _mockMedicationRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((list, 1));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items[0].Name.Should().Be(med.Name);
        }

        [Fact]
        public async Task GetMedicationByIdAsync_WhenExists_ReturnsDto()
        {
            // ARRANGE
            var med = TestDataBuilder.CreateMedication();
            _mockMedicationRepository.Setup(r => r.GetByIdAsync(med.Id)).ReturnsAsync(med);

            // ACT
            var result = await _sut.GetMedicationByIdAsync(med.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Id.Should().Be(med.Id);
            result.Name.Should().Be(med.Name);
        }

        [Fact]
        public async Task GetMedicationByIdAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockMedicationRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Medication?)null);

            // ACT
            var act = async () => await _sut.GetMedicationByIdAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetLowStockAsync_ReturnsMatchingMedications()
        {
            // ARRANGE
            var med = TestDataBuilder.CreateMedication(stockQuantity: 5);
            var list = new List<Medication> { med };
            _mockMedicationRepository.Setup(r => r.GetLowStockAsync(10)).ReturnsAsync(list);

            // ACT
            var result = await _sut.GetLowStockAsync(10);

            // ASSERT
            result.Should().HaveCount(1);
            result[0].StockQuantity.Should().Be(5);
        }

        [Fact]
        public async Task CreateMedicationAsync_WhenValid_ReturnsCreatedDto()
        {
            // ARRANGE
            var dto = TestDataBuilder.CreateMedicationDto();

            // ACT
            var result = await _sut.CreateMedicationAsync(dto);

            // ASSERT
            result.Should().NotBeNull();
            result.Name.Should().Be(dto.Name);
            _mockMedicationRepository.Verify(r => r.AddAsync(It.IsAny<Medication>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task CreateMedicationAsync_WhenValidationFails_ThrowsValidationException()
        {
            // ARRANGE
            var dto = TestDataBuilder.CreateMedicationDto();
            dto.Name = ""; // invalid

            // ACT
            var act = async () => await _sut.CreateMedicationAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        [Fact]
        public async Task UpdateMedicationAsync_WhenValid_UpdatesRecord()
        {
            // ARRANGE
            var med = TestDataBuilder.CreateMedication();
            var updateDto = TestDataBuilder.UpdateMedicationDto(med.Id);
            _mockMedicationRepository.Setup(r => r.GetByIdAsync(med.Id)).ReturnsAsync(med);

            // ACT
            await _sut.UpdateMedicationAsync(updateDto);

            // ASSERT
            med.Name.Should().Be(updateDto.Name);
            _mockMedicationRepository.Verify(r => r.UpdateAsync(med), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateMedicationAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var updateDto = TestDataBuilder.UpdateMedicationDto();
            _mockMedicationRepository.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync((Medication?)null);

            // ACT
            var act = async () => await _sut.UpdateMedicationAsync(updateDto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteMedicationAsync_WhenExists_DeletesRecord()
        {
            // ARRANGE
            var med = TestDataBuilder.CreateMedication();
            _mockMedicationRepository.Setup(r => r.GetByIdAsync(med.Id)).ReturnsAsync(med);

            // ACT
            await _sut.DeleteMedicationAsync(med.Id);

            // ASSERT
            _mockMedicationRepository.Verify(r => r.DeleteAsync(med), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteMedicationAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockMedicationRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Medication?)null);

            // ACT
            var act = async () => await _sut.DeleteMedicationAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
