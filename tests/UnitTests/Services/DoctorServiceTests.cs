using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Doctor;
using Hospital.Application.Exceptions;
using Hospital.Application.Mappings;
using Hospital.Application.Services;
using Hospital.Application.Validations;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using UnitTests.Helpers;

namespace UnitTests.Services
{
    // ─────────────────────────────────────────────────────────────────────────
    // DOCTORSERVICETESTS
    //
    // Doctors are more complex than Departments because they have a FOREIGN KEY
    // to Department. This introduces extra business rules we must test:
    //
    //   Rule 1: When creating a Doctor, the DepartmentId must reference a real Department.
    //   Rule 2: When updating a Doctor, if DepartmentId changes, the new one must exist.
    //   Rule 3: GetById must use the eager-loading version (GetByIdWithDepartmentAsync)
    //           so the response includes DepartmentName, not just DepartmentId.
    //
    // These cross-entity rules are the most important things to test in this service.
    // ─────────────────────────────────────────────────────────────────────────
    public class DoctorServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IDoctorRepository> _mockDoctorRepository;
        private readonly Mock<IDepartmentRepository> _mockDepartmentRepository;
        private readonly IMapper _mapper;
        private readonly DoctorService _sut;

        public DoctorServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockDoctorRepository = new Mock<IDoctorRepository>();
            _mockDepartmentRepository = new Mock<IDepartmentRepository>();

            // Wire up both repositories to the UnitOfWork mock
            _mockUnitOfWork.Setup(u => u.Doctors).Returns(_mockDoctorRepository.Object);
            _mockUnitOfWork.Setup(u => u.Departments).Returns(_mockDepartmentRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Real AutoMapper with BOTH DoctorProfile and DepartmentProfile.
            // DoctorProfile maps Doctor.Department.Name → DoctorDto.DepartmentName.
            // We MUST include DepartmentProfile too because AutoMapper validates
            // all registered profiles for consistency at MapperConfiguration build time.
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<DoctorProfile>();
                cfg.AddProfile<DepartmentProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<CreateDoctorDto> createValidator = new CreateDoctorDtoValidator();
            IValidator<UpdateDoctorDto> updateValidator = new UpdateDoctorDtoValidator();

            _sut = new DoctorService(
                _mockUnitOfWork.Object,
                _mapper,
                createValidator,
                updateValidator);
        }

        // ════════════════════════════════════════════════════════════════════
        // GET DOCTOR BY ID
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetDoctorByIdAsync_WhenDoctorExists_ReturnsDoctorDtoWithDepartmentName()
        {
            // ARRANGE
            // CreateDoctor pre-populates Doctor.Department so DoctorProfile can map
            // Doctor.Department.Name → DoctorDto.DepartmentName without null crash.
            var doctor = TestDataBuilder.CreateDoctor(firstName: "Ahmed", lastName: "Hassan");

            // DoctorService uses GetByIdWithDepartmentAsync (not plain GetByIdAsync)
            // to eagerly load the Department navigation property.
            _mockDoctorRepository
                .Setup(r => r.GetByIdWithDepartmentAsync(doctor.Id))
                .ReturnsAsync(doctor);

            // ACT
            var result = await _sut.GetDoctorByIdAsync(doctor.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.FirstName.Should().Be("Ahmed");
            result.LastName.Should().Be("Hassan");

            // Critical: verify DepartmentName was mapped correctly.
            // This is only possible if GetByIdWithDepartmentAsync is called (not plain GetByIdAsync).
            // If the wrong repository method was called, Department would be null
            // and this assertion would fail — catching a real service bug.
            result.DepartmentName.Should().Be("Cardiology");
        }

        [Fact]
        public async Task GetDoctorByIdAsync_WhenDoctorDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var nonExistentId = Guid.NewGuid();
            _mockDoctorRepository
                .Setup(r => r.GetByIdWithDepartmentAsync(nonExistentId))
                .ReturnsAsync((Doctor?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.GetDoctorByIdAsync(nonExistentId))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Doctor*");
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE DOCTOR
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateDoctorAsync_WhenDepartmentExists_CreatesDoctorSuccessfully()
        {
            // ARRANGE
            var department = TestDataBuilder.CreateDepartment();
            var createDto = TestDataBuilder.CreateDoctorDto(departmentId: department.Id);

            // The service first checks that DepartmentId references a real department.
            // We mock that lookup to return a real department (the happy path).
            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(department.Id))
                .ReturnsAsync(department);

            // After AddAsync, the service calls GetByIdWithDepartmentAsync to reload
            // the entity with Department included, so the response DTO has DepartmentName.
            _mockDoctorRepository
                .Setup(r => r.AddAsync(It.IsAny<Doctor>()))
                .ReturnsAsync((Doctor entity) => entity);

            // We need to mock GetByIdWithDepartmentAsync for the post-create reload.
            // The service calls this after save to build the response DTO.
            // It.IsAny<Guid>() matches whatever Id the newly created Doctor received.
            _mockDoctorRepository
                .Setup(r => r.GetByIdWithDepartmentAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => TestDataBuilder.CreateDoctor(id: id, departmentId: department.Id));

            // ACT
            var result = await _sut.CreateDoctorAsync(createDto);

            // ASSERT
            result.Should().NotBeNull();
            result.DepartmentName.Should().Be("Cardiology");

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateDoctorAsync_WhenDepartmentDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE — the DepartmentId in the DTO doesn't exist in the database
            var createDto = TestDataBuilder.CreateDoctorDto();

            // Mock: department not found → returns null
            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Department?)null);

            // ACT + ASSERT
            // This is the critical business rule:
            // You cannot create a doctor for a department that doesn't exist.
            // Without this check, EF would throw a FK constraint violation
            // at the database level — a confusing 500 error instead of a 404.
            await _sut.Invoking(s => s.CreateDoctorAsync(createDto))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Department*");

            // Nothing should be saved
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateDoctorAsync_WithEmptyFirstName_ThrowsValidationException()
        {
            // ARRANGE — invalid: first name required
            var invalidDto = new CreateDoctorDto
            {
                FirstName = "",   // ← violates NotEmpty rule
                LastName = "Hassan",
                Specialization = "Cardiology",
                LicenseNumber = "LIC-001",
                YearsOfExperience = 5,
                ContactNumber = "+1234567890",
                DepartmentId = Guid.NewGuid()
            };

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateDoctorAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            // Verify department existence was never checked — validation runs first
            _mockDepartmentRepository.Verify(r =>
                r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateDoctorAsync_WithNegativeYearsOfExperience_ThrowsValidationException()
        {
            // ARRANGE — YearsOfExperience must be >= 0
            var invalidDto = TestDataBuilder.CreateDoctorDto();
            invalidDto.YearsOfExperience = -1; // ← violates GreaterThanOrEqualTo(0) rule

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateDoctorAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateDoctorAsync_WithEmptyDepartmentId_ThrowsValidationException()
        {
            // ARRANGE — DepartmentId is Guid.Empty which fails NotEmpty validation
            var invalidDto = TestDataBuilder.CreateDoctorDto(departmentId: Guid.Empty);

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateDoctorAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE DOCTOR
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateDoctorAsync_WhenDoctorExistsAndDepartmentUnchanged_UpdatesSuccessfully()
        {
            // ARRANGE
            var existingDoctor = TestDataBuilder.CreateDoctor();

            // The update DTO uses the SAME DepartmentId as the existing doctor.
            // This exercises the "department didn't change, skip the dept check" branch.
            var updateDto = TestDataBuilder.UpdateDoctorDto(
                id: existingDoctor.Id,
                departmentId: existingDoctor.DepartmentId);

            _mockDoctorRepository
                .Setup(r => r.GetByIdAsync(existingDoctor.Id))
                .ReturnsAsync(existingDoctor);

            // ACT
            await _sut.UpdateDoctorAsync(updateDto);

            // ASSERT
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Department lookup should NOT happen because DepartmentId didn't change.
            // This tests the optimisation in DoctorService:
            //   "Only validate the new DepartmentId if it actually changed"
            _mockDepartmentRepository.Verify(r =>
                r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task UpdateDoctorAsync_WhenDepartmentChangedAndNewDeptExists_UpdatesSuccessfully()
        {
            // ARRANGE — doctor exists, but we're moving them to a NEW department
            var existingDoctor = TestDataBuilder.CreateDoctor();
            var newDepartment = TestDataBuilder.CreateDepartment(name: "Neurology");

            // Update DTO has a DIFFERENT DepartmentId → triggers department validation
            var updateDto = TestDataBuilder.UpdateDoctorDto(
                id: existingDoctor.Id,
                departmentId: newDepartment.Id);

            _mockDoctorRepository
                .Setup(r => r.GetByIdAsync(existingDoctor.Id))
                .ReturnsAsync(existingDoctor);

            // The new department exists — validation should pass
            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(newDepartment.Id))
                .ReturnsAsync(newDepartment);

            // ACT
            await _sut.UpdateDoctorAsync(updateDto);

            // ASSERT
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateDoctorAsync_WhenDepartmentChangedAndNewDeptDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE — doctor exists, but new DepartmentId doesn't
            var existingDoctor = TestDataBuilder.CreateDoctor();
            var nonExistentDeptId = Guid.NewGuid();

            var updateDto = TestDataBuilder.UpdateDoctorDto(
                id: existingDoctor.Id,
                departmentId: nonExistentDeptId); // different from existing

            _mockDoctorRepository
                .Setup(r => r.GetByIdAsync(existingDoctor.Id))
                .ReturnsAsync(existingDoctor);

            // New department not found
            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(nonExistentDeptId))
                .ReturnsAsync((Department?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.UpdateDoctorAsync(updateDto))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Department*");

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateDoctorAsync_WhenDoctorDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var updateDto = TestDataBuilder.UpdateDoctorDto();
            _mockDoctorRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Doctor?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.UpdateDoctorAsync(updateDto))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Doctor*");
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE DOCTOR
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteDoctorAsync_WhenDoctorExists_DeletesSuccessfully()
        {
            // ARRANGE
            var doctor = TestDataBuilder.CreateDoctor();
            _mockDoctorRepository
                .Setup(r => r.GetByIdAsync(doctor.Id))
                .ReturnsAsync(doctor);

            // ACT
            await _sut.DeleteDoctorAsync(doctor.Id);

            // ASSERT
            _mockDoctorRepository.Verify(r => r.DeleteAsync(doctor), Times.Once);
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteDoctorAsync_WhenDoctorDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            _mockDoctorRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Doctor?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.DeleteDoctorAsync(Guid.NewGuid()))
                .Should().ThrowAsync<NotFoundException>();

            _mockDoctorRepository.Verify(r =>
                r.DeleteAsync(It.IsAny<Doctor>()), Times.Never);
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
