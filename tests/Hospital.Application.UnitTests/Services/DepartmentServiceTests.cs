using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hospital.Application.DTOs.Department;
using Hospital.Application.Exceptions;
using Hospital.Application.Services;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Moq;
using Xunit;

// Alias to disambiguate: our ValidationException vs FluentValidation.ValidationException
using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.UnitTests.Services
{
    public class DepartmentServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CreateDepartmentDto>> _mockCreateValidator;
        private readonly Mock<IValidator<UpdateDepartmentDto>> _mockUpdateValidator;
        private readonly DepartmentService _departmentService;

        public DepartmentServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCreateValidator = new Mock<IValidator<CreateDepartmentDto>>();
            _mockUpdateValidator = new Mock<IValidator<UpdateDepartmentDto>>();

            _departmentService = new DepartmentService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockCreateValidator.Object,
                _mockUpdateValidator.Object);
        }

        [Fact]
        public async Task GetAllDepartmentsAsync_ShouldReturnListOfDepartmentDtos()
        {
            // Arrange
            var departments = new List<Department>
            {
                new Department { Id = Guid.NewGuid(), Name = "Cardiology" },
                new Department { Id = Guid.NewGuid(), Name = "Neurology" }
            };

            var departmentDtos = new List<DepartmentDto>
            {
                new DepartmentDto { Id = departments[0].Id, Name = "Cardiology" },
                new DepartmentDto { Id = departments[1].Id, Name = "Neurology" }
            };

            _mockUnitOfWork.Setup(u => u.Departments.GetAllAsync()).ReturnsAsync(departments);
            _mockMapper.Setup(m => m.Map<IEnumerable<DepartmentDto>>(departments)).Returns(departmentDtos);

            // Act
            var result = await _departmentService.GetAllDepartmentsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(departmentDtos);
            _mockUnitOfWork.Verify(u => u.Departments.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_ExistingId_ShouldReturnDepartmentDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var department = new Department { Id = id, Name = "Cardiology" };
            var departmentDto = new DepartmentDto { Id = id, Name = "Cardiology" };

            _mockUnitOfWork.Setup(u => u.Departments.GetByIdAsync(id)).ReturnsAsync(department);
            _mockMapper.Setup(m => m.Map<DepartmentDto>(department)).Returns(departmentDto);

            // Act
            var result = await _departmentService.GetDepartmentByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(id);
            result.Name.Should().Be("Cardiology");
            _mockUnitOfWork.Verify(u => u.Departments.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_NonExistingId_ShouldThrowNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockUnitOfWork.Setup(u => u.Departments.GetByIdAsync(id)).ReturnsAsync((Department)null);

            // Act
            Func<Task> act = async () => await _departmentService.GetDepartmentByIdAsync(id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _mockUnitOfWork.Verify(u => u.Departments.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task CreateDepartmentAsync_ValidDto_ShouldCreateAndReturnDepartmentDto()
        {
            // Arrange
            var createDto = new CreateDepartmentDto { Name = "Orthopedics", Description = "Bones" };
            var department = new Department { Name = "Orthopedics", Description = "Bones" };
            var departmentDto = new DepartmentDto { Id = Guid.NewGuid(), Name = "Orthopedics", Description = "Bones" };

            _mockCreateValidator
                .Setup(v => v.ValidateAsync(createDto, default))
                .ReturnsAsync(new ValidationResult());

            _mockMapper.Setup(m => m.Map<Department>(createDto)).Returns(department);
            _mockMapper.Setup(m => m.Map<DepartmentDto>(department)).Returns(departmentDto);
            _mockUnitOfWork.Setup(u => u.Departments.AddAsync(It.IsAny<Department>())).ReturnsAsync(department);

            // Act
            var result = await _departmentService.CreateDepartmentAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Orthopedics");
            
            _mockUnitOfWork.Verify(u => u.Departments.AddAsync(It.IsAny<Department>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateDepartmentAsync_InvalidDto_ShouldThrowValidationException()
        {
            // Arrange
            var createDto = new CreateDepartmentDto();
            var validationResult = new ValidationResult(new List<ValidationFailure>
            {
                new ValidationFailure("Name", "Name is required")
            });

            _mockCreateValidator
                .Setup(v => v.ValidateAsync(createDto, default))
                .ReturnsAsync(validationResult);

            // Act
            Func<Task> act = async () => await _departmentService.CreateDepartmentAsync(createDto);

            // Assert
            await act.Should().ThrowAsync<AppValidationException>();
            _mockUnitOfWork.Verify(u => u.Departments.AddAsync(It.IsAny<Department>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }
}
