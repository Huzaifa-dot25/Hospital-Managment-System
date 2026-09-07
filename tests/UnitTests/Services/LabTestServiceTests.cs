using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Hospital.Application.DTOs.Laboratory;
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

namespace UnitTests.Services
{
    public class LabTestServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILabTestRepository> _mockLabTestRepository;
        private readonly IMapper _mapper;
        private readonly LabTestService _sut;

        public LabTestServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLabTestRepository = new Mock<ILabTestRepository>();

            _mockUnitOfWork.Setup(u => u.LabTests).Returns(_mockLabTestRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<LaboratoryProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<CreateLabTestDto> createValidator = new CreateLabTestDtoValidator();
            IValidator<UpdateLabTestDto> updateValidator = new UpdateLabTestDtoValidator();

            _sut = new LabTestService(
                _mockUnitOfWork.Object,
                _mapper,
                createValidator,
                updateValidator);
        }

        [Fact]
        public async Task GetPagedAsync_WhenLabTestsExist_ReturnsPagedResponse()
        {
            // ARRANGE
            var test = TestDataBuilder.CreateLabTest();
            var queryParams = new LabTestQueryParams { PageNumber = 1, PageSize = 10 };

            _mockLabTestRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((new List<LabTest> { test }, 1));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(t => t.Code == test.Code);
        }

        [Fact]
        public async Task GetByIdAsync_WhenTestExists_ReturnsDto()
        {
            // ARRANGE
            var test = TestDataBuilder.CreateLabTest();
            _mockLabTestRepository
                .Setup(r => r.GetByIdAsync(test.Id))
                .ReturnsAsync(test);

            // ACT
            var result = await _sut.GetByIdAsync(test.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Id.Should().Be(test.Id);
            result.Code.Should().Be(test.Code);
            result.Name.Should().Be(test.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WhenTestDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockLabTestRepository
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((LabTest?)null);

            // ACT
            Func<Task> act = async () => await _sut.GetByIdAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ReturnsCreatedDto()
        {
            // ARRANGE
            var dto = TestDataBuilder.CreateLabTestDto();
            _mockLabTestRepository
                .Setup(r => r.ExistsByCodeAsync(dto.Code, null))
                .ReturnsAsync(false);

            _mockLabTestRepository
                .Setup(r => r.AddAsync(It.IsAny<LabTest>()))
                .ReturnsAsync((LabTest t) => t);

            // ACT
            var result = await _sut.CreateAsync(dto);

            // ASSERT
            result.Should().NotBeNull();
            result.Code.Should().Be(dto.Code);
            result.Name.Should().Be(dto.Name);

            _mockLabTestRepository.Verify(r => r.AddAsync(It.IsAny<LabTest>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenCodeAlreadyExists_ThrowsBadRequestException()
        {
            // ARRANGE
            var dto = TestDataBuilder.CreateLabTestDto();
            _mockLabTestRepository
                .Setup(r => r.ExistsByCodeAsync(dto.Code, null))
                .ReturnsAsync(true);

            // ACT
            Func<Task> act = async () => await _sut.CreateAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage($"*{dto.Code}*");
        }

        [Fact]
        public async Task CreateAsync_WithInvalidData_ThrowsValidationException()
        {
            // ARRANGE
            var dto = new CreateLabTestDto { Name = "" }; // Missing Code, Price, etc.

            // ACT
            Func<Task> act = async () => await _sut.CreateAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_CallsUpdateAndSave()
        {
            // ARRANGE
            var test = TestDataBuilder.CreateLabTest();
            var updateDto = TestDataBuilder.CreateUpdateLabTestDto(test.Id);

            _mockLabTestRepository
                .Setup(r => r.GetByIdAsync(test.Id))
                .ReturnsAsync(test);

            _mockLabTestRepository
                .Setup(r => r.ExistsByCodeAsync(updateDto.Code, updateDto.Id))
                .ReturnsAsync(false);

            // ACT
            await _sut.UpdateAsync(updateDto);

            // ASSERT
            _mockLabTestRepository.Verify(r => r.UpdateAsync(test), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var updateDto = TestDataBuilder.CreateUpdateLabTestDto();
            _mockLabTestRepository
                .Setup(r => r.GetByIdAsync(updateDto.Id))
                .ReturnsAsync((LabTest?)null);

            // ACT
            Func<Task> act = async () => await _sut.UpdateAsync(updateDto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateAsync_WhenCodeConflicts_ThrowsBadRequestException()
        {
            // ARRANGE
            var test = TestDataBuilder.CreateLabTest();
            var updateDto = TestDataBuilder.CreateUpdateLabTestDto(test.Id);

            _mockLabTestRepository
                .Setup(r => r.GetByIdAsync(test.Id))
                .ReturnsAsync(test);

            _mockLabTestRepository
                .Setup(r => r.ExistsByCodeAsync(updateDto.Code, updateDto.Id))
                .ReturnsAsync(true);

            // ACT
            Func<Task> act = async () => await _sut.UpdateAsync(updateDto);

            // ASSERT
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage($"*{updateDto.Code}*");
        }

        [Fact]
        public async Task DeleteAsync_WhenExists_CallsDeleteAndSave()
        {
            // ARRANGE
            var test = TestDataBuilder.CreateLabTest();
            _mockLabTestRepository
                .Setup(r => r.GetByIdAsync(test.Id))
                .ReturnsAsync(test);

            // ACT
            await _sut.DeleteAsync(test.Id);

            // ASSERT
            _mockLabTestRepository.Verify(r => r.DeleteAsync(test), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockLabTestRepository
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((LabTest?)null);

            // ACT
            Func<Task> act = async () => await _sut.DeleteAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
