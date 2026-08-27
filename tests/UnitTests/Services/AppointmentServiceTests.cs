using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Appointment;
using Hospital.Application.Exceptions;
using Hospital.Application.Mappings;
using Hospital.Application.Services;
using Hospital.Application.Validations;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Repositories;
using Hospital.Shared.Queries;
using UnitTests.Helpers;

namespace UnitTests.Services
{
    // ─────────────────────────────────────────────────────────────────────────
    // APPOINTMENTSERVICETESTS
    //
    // Appointments are the most complex service because they involve THREE entities:
    // Appointment, Patient, and Doctor. The business rules are:
    //
    //   Rule 1: PatientId must reference a real Patient before creating.
    //   Rule 2: DoctorId must reference a real Doctor before creating.
    //   Rule 3: AppointmentDate must be in the FUTURE (validated by FluentValidation).
    //   Rule 4: After creation, the appointment must be reloaded with Patient+Doctor
    //           so the response DTO includes PatientName and DoctorName.
    //   Rule 5: Updates do NOT re-validate Patient/Doctor existence — only the
    //           appointment itself must exist.
    //
    // These rules map directly to tests below.
    // ─────────────────────────────────────────────────────────────────────────
    public class AppointmentServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAppointmentRepository> _mockAppointmentRepository;
        private readonly Mock<IPatientRepository> _mockPatientRepository;
        private readonly Mock<IDoctorRepository> _mockDoctorRepository;
        private readonly IMapper _mapper;
        private readonly AppointmentService _sut;

        public AppointmentServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAppointmentRepository = new Mock<IAppointmentRepository>();
            _mockPatientRepository = new Mock<IPatientRepository>();
            _mockDoctorRepository = new Mock<IDoctorRepository>();

            // Wire all three repositories to UnitOfWork
            _mockUnitOfWork.Setup(u => u.Appointments).Returns(_mockAppointmentRepository.Object);
            _mockUnitOfWork.Setup(u => u.Patients).Returns(_mockPatientRepository.Object);
            _mockUnitOfWork.Setup(u => u.Doctors).Returns(_mockDoctorRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Real AutoMapper with all related profiles.
            // AppointmentProfile uses Patient and Doctor navigation properties,
            // so we must include PatientProfile and DoctorProfile too — even though
            // we don't use them directly. AutoMapper validates all registered profiles
            // against each other and throws at startup if any is inconsistent.
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AppointmentProfile>();
                cfg.AddProfile<PatientProfile>();
                cfg.AddProfile<DoctorProfile>();
                cfg.AddProfile<DepartmentProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<CreateAppointmentDto> createValidator = new CreateAppointmentDtoValidator();
            IValidator<UpdateAppointmentDto> updateValidator = new UpdateAppointmentDtoValidator();

            _sut = new AppointmentService(
                _mockUnitOfWork.Object,
                _mapper,
                createValidator,
                updateValidator);
        }

        // ════════════════════════════════════════════════════════════════════
        // GET APPOINTMENT BY ID
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAppointmentByIdAsync_WhenExists_ReturnsAppointmentDtoWithPatientAndDoctorNames()
        {
            // ARRANGE
            // CreateAppointment pre-populates navigation properties (Patient + Doctor)
            // so AutoMapper can compute PatientName and DoctorName in AppointmentProfile.
            var appointment = TestDataBuilder.CreateAppointment();

            // AppointmentService uses GetByIdWithDetailsAsync (the eager-loading version)
            _mockAppointmentRepository
                .Setup(r => r.GetByIdWithDetailsAsync(appointment.Id))
                .ReturnsAsync(appointment);

            // ACT
            var result = await _sut.GetAppointmentByIdAsync(appointment.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Id.Should().Be(appointment.Id);

            // PatientName = "Sara Ahmed" (from AppointmentProfile mapping)
            result.PatientName.Should().Be("Sara Ahmed");

            // DoctorName = "Dr. Ahmed Hassan" (from AppointmentProfile mapping)
            result.DoctorName.Should().Be("Dr. Ahmed Hassan");
        }

        [Fact]
        public async Task GetAppointmentByIdAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var nonExistentId = Guid.NewGuid();
            _mockAppointmentRepository
                .Setup(r => r.GetByIdWithDetailsAsync(nonExistentId))
                .ReturnsAsync((Appointment?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.GetAppointmentByIdAsync(nonExistentId))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Appointment*");
        }

        // ════════════════════════════════════════════════════════════════════
        // GET PAGED
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetPagedAsync_WhenAppointmentsExist_ReturnsCorrectPageMetadata()
        {
            // ARRANGE
            var appointments = new List<Appointment>
            {
                TestDataBuilder.CreateAppointment(),
                TestDataBuilder.CreateAppointment()
            };

            var queryParams = new AppointmentQueryParams { PageNumber = 1, PageSize = 5 };

            _mockAppointmentRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((appointments.AsReadOnly(), 2));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.TotalPages.Should().Be(1); // 2 items, 5 per page → 1 page
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE APPOINTMENT
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateAppointmentAsync_WhenPatientAndDoctorExist_CreatesSuccessfully()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var doctor = TestDataBuilder.CreateDoctor();
            var createDto = TestDataBuilder.CreateAppointmentDto(
                patientId: patient.Id,
                doctorId: doctor.Id);

            // Both patient and doctor exist in the "database"
            _mockPatientRepository
                .Setup(r => r.GetByIdAsync(patient.Id))
                .ReturnsAsync(patient);
            _mockDoctorRepository
                .Setup(r => r.GetByIdAsync(doctor.Id))
                .ReturnsAsync(doctor);

            _mockAppointmentRepository
                .Setup(r => r.AddAsync(It.IsAny<Appointment>()))
                .ReturnsAsync((Appointment entity) => entity);

            // After save, the service reloads with GetByIdWithDetailsAsync
            // so PatientName and DoctorName appear in the response.
            _mockAppointmentRepository
                .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                    TestDataBuilder.CreateAppointment(
                        id: id,
                        patientId: patient.Id,
                        doctorId: doctor.Id));

            // ACT
            var result = await _sut.CreateAppointmentAsync(createDto);

            // ASSERT
            result.Should().NotBeNull();
            result.PatientName.Should().Be("Sara Ahmed");
            result.DoctorName.Should().Be("Dr. Ahmed Hassan");

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WhenPatientDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var createDto = TestDataBuilder.CreateAppointmentDto();

            // Patient not found — this is the first existence check in the service
            _mockPatientRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Patient?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateAppointmentAsync(createDto))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Patient*");

            // Doctor lookup should never happen — we fail fast on Patient check
            _mockDoctorRepository.Verify(r =>
                r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WhenDoctorDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var createDto = TestDataBuilder.CreateAppointmentDto(patientId: patient.Id);

            // Patient found but Doctor not found
            _mockPatientRepository
                .Setup(r => r.GetByIdAsync(patient.Id))
                .ReturnsAsync(patient);

            _mockDoctorRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Doctor?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateAppointmentAsync(createDto))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Doctor*");

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithPastAppointmentDate_ThrowsValidationException()
        {
            // ARRANGE — appointment in the past violates GreaterThan(DateTime.UtcNow) rule
            var createDto = TestDataBuilder.CreateAppointmentDto();
            createDto.AppointmentDate = DateTime.UtcNow.AddDays(-1); // ← past date

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateAppointmentAsync(createDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            // Validation runs BEFORE any existence checks — nothing else called
            _mockPatientRepository.Verify(r =>
                r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithEmptyReason_ThrowsValidationException()
        {
            // ARRANGE
            var createDto = TestDataBuilder.CreateAppointmentDto();
            createDto.Reason = ""; // ← required field

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateAppointmentAsync(createDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        [Fact]
        public async Task CreateAppointmentAsync_WithEmptyPatientId_ThrowsValidationException()
        {
            // ARRANGE — Guid.Empty fails NotEmpty validation
            var createDto = TestDataBuilder.CreateAppointmentDto(patientId: Guid.Empty);

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateAppointmentAsync(createDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE APPOINTMENT
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateAppointmentAsync_WhenAppointmentExists_UpdatesSuccessfully()
        {
            // ARRANGE
            var existing = TestDataBuilder.CreateAppointment();
            var updateDto = TestDataBuilder.UpdateAppointmentDto(id: existing.Id);

            _mockAppointmentRepository
                .Setup(r => r.GetByIdAsync(existing.Id))
                .ReturnsAsync(existing);

            // ACT
            await _sut.UpdateAppointmentAsync(updateDto);

            // ASSERT
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_WhenAppointmentDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var updateDto = TestDataBuilder.UpdateAppointmentDto();
            _mockAppointmentRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Appointment?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.UpdateAppointmentAsync(updateDto))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Appointment*");

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_WithEmptyReason_ThrowsValidationException()
        {
            // ARRANGE — reason is required for update too
            var invalidDto = new UpdateAppointmentDto
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                Reason = "",      // ← invalid
                Status = AppointmentStatus.Scheduled
            };

            // ACT + ASSERT
            await _sut.Invoking(s => s.UpdateAppointmentAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            // No DB calls before validation fails
            _mockAppointmentRepository.Verify(r =>
                r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_StatusCanBeChangedToCompleted()
        {
            // ARRANGE — testing that status update (Scheduled → Completed) works
            var existing = TestDataBuilder.CreateAppointment(status: AppointmentStatus.Scheduled);
            var updateDto = TestDataBuilder.UpdateAppointmentDto(id: existing.Id);
            updateDto.Status = AppointmentStatus.Completed; // ← status change

            _mockAppointmentRepository
                .Setup(r => r.GetByIdAsync(existing.Id))
                .ReturnsAsync(existing);

            // ACT — should not throw
            await _sut.UpdateAppointmentAsync(updateDto);

            // ASSERT — saved successfully
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE APPOINTMENT
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteAppointmentAsync_WhenExists_DeletesSuccessfully()
        {
            // ARRANGE
            var appointment = TestDataBuilder.CreateAppointment();
            _mockAppointmentRepository
                .Setup(r => r.GetByIdAsync(appointment.Id))
                .ReturnsAsync(appointment);

            // ACT
            await _sut.DeleteAppointmentAsync(appointment.Id);

            // ASSERT
            _mockAppointmentRepository.Verify(r =>
                r.DeleteAsync(appointment), Times.Once);
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            _mockAppointmentRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Appointment?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.DeleteAppointmentAsync(Guid.NewGuid()))
                .Should().ThrowAsync<NotFoundException>();

            _mockAppointmentRepository.Verify(r =>
                r.DeleteAsync(It.IsAny<Appointment>()), Times.Never);
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
