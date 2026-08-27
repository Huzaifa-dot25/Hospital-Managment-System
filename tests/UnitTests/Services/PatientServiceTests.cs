using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Patient;
using Hospital.Application.Exceptions;
using Hospital.Application.Mappings;
using Hospital.Application.Services;
using Hospital.Application.Validations;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using UnitTests.Helpers;

namespace UnitTests.Services
{
    // ─────────────────────────────────────────────────────────────────────────
    // PATIENTSERVICETESTS
    //
    // The Patient module is the most critical in the system.
    // A bug here means wrong patient data, wrong medical records, or worse —
    // treating the wrong person.
    //
    // Key things we test beyond basic CRUD:
    //
    //   1. Pagination — GetPagedAsync returns correct metadata (TotalCount, TotalPages)
    //   2. Soft delete — DeleteAsync triggers the right calls (NOT hard delete)
    //   3. Validation — all required fields and format rules are enforced
    //   4. Mapping completeness — FullName and Age computed properties work
    // ─────────────────────────────────────────────────────────────────────────
    public class PatientServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IPatientRepository> _mockPatientRepository;
        private readonly IMapper _mapper;
        private readonly PatientService _sut;

        public PatientServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockPatientRepository = new Mock<IPatientRepository>();

            _mockUnitOfWork.Setup(u => u.Patients).Returns(_mockPatientRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<PatientProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<CreatePatientDto> createValidator = new CreatePatientDtoValidator();
            IValidator<UpdatePatientDto> updateValidator = new UpdatePatientDtoValidator();

            _sut = new PatientService(
                _mockUnitOfWork.Object,
                _mapper,
                createValidator,
                updateValidator);
        }

        // ════════════════════════════════════════════════════════════════════
        // GET PAGED
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetPagedAsync_WhenPatientsExist_ReturnsPagedResponseWithCorrectMetadata()
        {
            // ARRANGE
            // Create 3 patients but simulate a total count of 10 (like having 10 in DB,
            // and the current page shows 3 of them).
            var patients = new List<Patient>
            {
                TestDataBuilder.CreatePatient(firstName: "Sara"),
                TestDataBuilder.CreatePatient(firstName: "Nour"),
                TestDataBuilder.CreatePatient(firstName: "Hana")
            };

            var queryParams = new PatientQueryParams { PageNumber = 1, PageSize = 3 };
            var totalCount = 10; // Total records in the "database"

            _mockPatientRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((patients.AsReadOnly(), totalCount));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.Should().NotBeNull();

            // Verify pagination metadata is calculated correctly:
            // 10 total records ÷ 3 per page = 4 pages (ceiling)
            result.TotalCount.Should().Be(10);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(3);
            result.TotalPages.Should().Be(4);

            // Verify the data items themselves
            result.Items.Should().HaveCount(3);
            result.Items.Should().Contain(p => p.FirstName == "Sara");
        }

        [Fact]
        public async Task GetPagedAsync_WhenNoPatientsExist_ReturnsEmptyPagedResponse()
        {
            // ARRANGE
            var queryParams = new PatientQueryParams { PageNumber = 1, PageSize = 10 };

            _mockPatientRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((new List<Patient>().AsReadOnly(), 0));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
        }

        // ════════════════════════════════════════════════════════════════════
        // GET BY ID
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetPatientByIdAsync_WhenPatientExists_ReturnsPatientDtoWithComputedAge()
        {
            // ARRANGE
            // DateOfBirth 30 years ago → Age should be 30 (PatientDto.Age is computed)
            var patient = TestDataBuilder.CreatePatient();
            // Override DoB to exactly 30 years ago for a deterministic age assertion
            patient.DateOfBirth = DateTime.UtcNow.AddYears(-30);

            _mockPatientRepository
                .Setup(r => r.GetByIdAsync(patient.Id))
                .ReturnsAsync(patient);

            // ACT
            var result = await _sut.GetPatientByIdAsync(patient.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Id.Should().Be(patient.Id);
            result.FirstName.Should().Be("Sara");

            // FullName is a computed property on PatientDto — not stored in DB.
            // If this fails, either AutoMapper mapping is broken or FullName logic is wrong.
            result.FullName.Should().Be("Sara Ahmed");

            // Age is computed from DateOfBirth — must equal 30 for our test data
            result.Age.Should().Be(30);
        }

        [Fact]
        public async Task GetPatientByIdAsync_WhenPatientDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var nonExistentId = Guid.NewGuid();
            _mockPatientRepository
                .Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Patient?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.GetPatientByIdAsync(nonExistentId))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Patient*");
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE PATIENT
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreatePatientAsync_WithValidData_ReturnsPatientDto()
        {
            // ARRANGE
            var createDto = TestDataBuilder.CreatePatientDto();

            _mockPatientRepository
                .Setup(r => r.AddAsync(It.IsAny<Patient>()))
                .ReturnsAsync((Patient entity) => entity);

            // ACT
            var result = await _sut.CreatePatientAsync(createDto);

            // ASSERT
            result.Should().NotBeNull();
            result.FirstName.Should().Be("Sara");
            result.LastName.Should().Be("Ahmed");

            _mockPatientRepository.Verify(r =>
                r.AddAsync(It.IsAny<Patient>()), Times.Once);
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreatePatientAsync_WithEmptyFirstName_ThrowsValidationException()
        {
            // ARRANGE
            var invalidDto = TestDataBuilder.CreatePatientDto(firstName: "");

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreatePatientAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreatePatientAsync_WithFutureDateOfBirth_ThrowsValidationException()
        {
            // ARRANGE — DateOfBirth in the future is biologically impossible
            var invalidDto = TestDataBuilder.CreatePatientDto();
            invalidDto.DateOfBirth = DateTime.UtcNow.AddDays(1); // ← future date

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreatePatientAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        [Fact]
        public async Task CreatePatientAsync_WithInvalidContactNumber_ThrowsValidationException()
        {
            // ARRANGE — contact number that fails the phone regex
            var invalidDto = TestDataBuilder.CreatePatientDto();
            invalidDto.ContactNumber = "not-a-phone"; // ← fails Matches regex

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreatePatientAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        [Fact]
        public async Task CreatePatientAsync_WithFirstNameExceeding50Chars_ThrowsValidationException()
        {
            // ARRANGE
            var invalidDto = TestDataBuilder.CreatePatientDto(firstName: new string('A', 51));

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreatePatientAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        [Fact]
        public async Task CreatePatientAsync_WithEmptyAddress_ThrowsValidationException()
        {
            // ARRANGE
            var invalidDto = TestDataBuilder.CreatePatientDto();
            invalidDto.Address = "";

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreatePatientAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE PATIENT
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdatePatientAsync_WhenPatientExists_UpdatesSuccessfully()
        {
            // ARRANGE
            var existingPatient = TestDataBuilder.CreatePatient();
            var updateDto = TestDataBuilder.UpdatePatientDto(id: existingPatient.Id);

            _mockPatientRepository
                .Setup(r => r.GetByIdAsync(existingPatient.Id))
                .ReturnsAsync(existingPatient);

            // ACT
            await _sut.UpdatePatientAsync(updateDto);

            // ASSERT
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePatientAsync_WhenPatientDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            var updateDto = TestDataBuilder.UpdatePatientDto();
            _mockPatientRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Patient?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.UpdatePatientAsync(updateDto))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Patient*");

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdatePatientAsync_WithInvalidData_ThrowsValidationExceptionBeforeDatabaseCall()
        {
            // ARRANGE — Empty first name
            var invalidDto = new UpdatePatientDto
            {
                Id = Guid.NewGuid(),
                FirstName = "",      // ← invalid
                LastName = "Ahmed",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                ContactNumber = "+1234567890",
                Address = "123 Street",
                EmergencyContactName = "Emergency",
                EmergencyContactNumber = "+9876543210"
            };

            // ACT + ASSERT
            await _sut.Invoking(s => s.UpdatePatientAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            // DB was never consulted — validation runs first, which is correct
            _mockPatientRepository.Verify(r =>
                r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE PATIENT
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeletePatientAsync_WhenPatientExists_CallsDeleteAndSave()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            _mockPatientRepository
                .Setup(r => r.GetByIdAsync(patient.Id))
                .ReturnsAsync(patient);

            // ACT
            await _sut.DeletePatientAsync(patient.Id);

            // ASSERT
            // The service calls DeleteAsync on the repository — it's the DbContext
            // that intercepts EntityState.Deleted and converts it to a soft delete.
            // Here we verify the repository was called, which is the service's responsibility.
            _mockPatientRepository.Verify(r => r.DeleteAsync(patient), Times.Once);
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeletePatientAsync_WhenPatientDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            _mockPatientRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Patient?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.DeletePatientAsync(Guid.NewGuid()))
                .Should().ThrowAsync<NotFoundException>();

            _mockPatientRepository.Verify(r =>
                r.DeleteAsync(It.IsAny<Patient>()), Times.Never);
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
