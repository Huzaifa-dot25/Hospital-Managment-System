using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Department;
using Hospital.Application.Exceptions;
using Hospital.Application.Mappings;
using Hospital.Application.Services;
using Hospital.Application.Validations.Department;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using UnitTests.Helpers;

namespace UnitTests.Services
{
    // ─────────────────────────────────────────────────────────────────────────
    // DEPARTMENTSERVICETESTS
    //
    // What we are testing:
    //   DepartmentService — the Application layer class that handles all
    //   department business logic: GetAll, GetById, Create, Update, Delete.
    //
    // What we are NOT testing (by design):
    //   - The database (mocked away via IUnitOfWork mock)
    //   - HTTP layer (no controllers here)
    //   - Infrastructure (no connection strings)
    //
    // Test naming convention:
    //   MethodName_StateUnderTest_ExpectedBehaviour
    //   Example: GetDepartmentByIdAsync_WhenDepartmentExists_ReturnsDepartmentDto
    //
    // This convention is the most widely used in .NET professional environments.
    // It reads like a sentence: "GetDepartmentById, when department exists, returns dto"
    // ─────────────────────────────────────────────────────────────────────────
    public class DepartmentServiceTests
    {
        // ── Fields ──────────────────────────────────────────────────────────
        // These are the mock objects and the REAL service we're testing.
        // They are declared as fields so every test method can access them.

        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IDepartmentRepository> _mockDepartmentRepository;
        private readonly IMapper _mapper;
        private readonly DepartmentService _sut; // SUT = System Under Test

        // ── Constructor ─────────────────────────────────────────────────────
        // xUnit creates a FRESH instance of this class for EVERY test method.
        // That means this constructor runs before EACH test.
        // This is how xUnit guarantees test isolation — no shared state.
        public DepartmentServiceTests()
        {
            // 1. Create the mock for the entire UnitOfWork
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            // 2. Create a separate mock for the DepartmentRepository
            //    (because IUnitOfWork exposes it as a property)
            _mockDepartmentRepository = new Mock<IDepartmentRepository>();

            // 3. Wire them together:
            //    When anyone accesses _unitOfWork.Departments, return our mock repo.
            //    This is how we intercept repository calls.
            _mockUnitOfWork
                .Setup(u => u.Departments)
                .Returns(_mockDepartmentRepository.Object);

            // 4. Setup SaveChangesAsync to return 1 (rows affected) by default.
            //    We don't want it to throw. If a test needs different behaviour,
            //    it can override this setup locally.
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // 5. Build the REAL AutoMapper using the real DepartmentProfile.
            //    We use the real mapper because we want to test the full flow,
            //    including the correctness of our AutoMapper configuration.
            //    A fake mapper would hide mapping bugs — we want to catch them here.
            //
            //    AutoMapper 15+ requires ILoggerFactory as a second argument.
            //    In tests we use NullLoggerFactory.Instance which silently discards
            //    all log output — perfect for tests (no console noise, no side effects).
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<DepartmentProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            // 6. Build the REAL validators from Hospital.Application.
            //    Same reasoning as above — we want to catch validation bugs here.
            IValidator<CreateDepartmentDto> createValidator = new CreateDepartmentDtoValidator();
            IValidator<UpdateDepartmentDto> updateValidator = new UpdateDepartmentDtoValidator();

            // 7. Create the System Under Test — the REAL DepartmentService.
            //    Notice we inject the MOCK unit of work and REAL mapper + validators.
            //    This is the heart of the unit test setup.
            _sut = new DepartmentService(
                _mockUnitOfWork.Object,   // .Object gives us the actual mock instance
                _mapper,
                createValidator,
                updateValidator
            );
        }

        // ════════════════════════════════════════════════════════════════════
        // GET ALL DEPARTMENTS
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAllDepartmentsAsync_WhenDepartmentsExist_ReturnsAllDepartments()
        {
            // ── ARRANGE ─────────────────────────────────────────────────────
            // Create two fake departments using our TestDataBuilder.
            // We're testing that BOTH get returned and mapped to DTOs.
            var departments = new List<Department>
            {
                TestDataBuilder.CreateDepartment(name: "Cardiology"),
                TestDataBuilder.CreateDepartment(name: "Neurology")
            };

            // Tell the mock: when GetAllAsync is called, return our fake list.
            // IReadOnlyList<Department> is what the repository interface returns.
            _mockDepartmentRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(departments.AsReadOnly());

            // ── ACT ──────────────────────────────────────────────────────────
            // Call the real service method — this is what we're actually testing.
            var result = await _sut.GetAllDepartmentsAsync();

            // ── ASSERT ───────────────────────────────────────────────────────
            // FluentAssertions makes these readable like English sentences.
            // result.Should().HaveCount(2) → verify 2 items were returned
            // result.Should().ContainSingle(d => d.Name == "Cardiology") → spot check
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(d => d.Name == "Cardiology");
            result.Should().Contain(d => d.Name == "Neurology");
        }

        [Fact]
        public async Task GetAllDepartmentsAsync_WhenNoDepartmentsExist_ReturnsEmptyList()
        {
            // ARRANGE — repository returns an empty list
            _mockDepartmentRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Department>().AsReadOnly());

            // ACT
            var result = await _sut.GetAllDepartmentsAsync();

            // ASSERT — empty collection, not null
            // This is important! Returning null for an empty list is a common bug
            // that crashes frontend applications. We should always return empty list.
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        // ════════════════════════════════════════════════════════════════════
        // GET DEPARTMENT BY ID
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetDepartmentByIdAsync_WhenDepartmentExists_ReturnsDepartmentDto()
        {
            // ARRANGE
            var department = TestDataBuilder.CreateDepartment(name: "Cardiology");

            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(department.Id))
                .ReturnsAsync(department);

            // ACT
            var result = await _sut.GetDepartmentByIdAsync(department.Id);

            // ASSERT
            // We verify:
            // 1. The result is not null
            // 2. The Id was correctly mapped
            // 3. The Name was correctly mapped
            // This proves the AutoMapper DepartmentProfile works for Entity → DTO.
            result.Should().NotBeNull();
            result.Id.Should().Be(department.Id);
            result.Name.Should().Be("Cardiology");
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_WhenDepartmentDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE
            // The repository returns null — simulating "not found in database"
            var nonExistentId = Guid.NewGuid();
            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Department?)null); // explicitly returning null

            // ACT + ASSERT
            // FluentAssertions: "Awaiting this method should throw a NotFoundException"
            // This verifies our service enforces the business rule:
            //   "If you request a department that doesn't exist, you get a 404"
            await _sut.Invoking(s => s.GetDepartmentByIdAsync(nonExistentId))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Department*");
            // *Department* means the error message contains the word "Department"
            // We don't hardcode the full message — that would make tests brittle.
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE DEPARTMENT
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateDepartmentAsync_WithValidData_ReturnsDepartmentDto()
        {
            // ARRANGE
            var createDto = TestDataBuilder.CreateDepartmentDto(name: "Radiology");

            // We mock AddAsync to capture and return the entity passed into it.
            // The lambda (entity => entity) means: return whatever was given to us.
            // This simulates EF Core behaviour: the entity gets its Id generated
            // and is returned after being tracked.
            _mockDepartmentRepository
                .Setup(r => r.AddAsync(It.IsAny<Department>()))
                .ReturnsAsync((Department entity) => entity);

            // ACT
            var result = await _sut.CreateDepartmentAsync(createDto);

            // ASSERT — verify the returned DTO has the data we sent
            result.Should().NotBeNull();
            result.Name.Should().Be("Radiology");

            // Verify that SaveChangesAsync was called exactly ONCE.
            // This is a behaviour verification — we're not just checking the output,
            // we're checking that the service actually committed the transaction.
            // If it wasn't called, data would be lost.
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateDepartmentAsync_WithEmptyName_ThrowsValidationException()
        {
            // ARRANGE — deliberately invalid: Name is required but we pass empty string
            var invalidDto = TestDataBuilder.CreateDepartmentDto(name: "");

            // ACT + ASSERT
            // Our CreateDepartmentDtoValidator requires Name.NotEmpty()
            // DepartmentService calls _createValidator.ValidateAsync() first,
            // and throws AppValidationException if invalid.
            await _sut.Invoking(s => s.CreateDepartmentAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            // Also verify that SaveChangesAsync was NEVER called.
            // Business rule: don't save if validation fails.
            // This prevents partial/corrupt data from reaching the database.
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateDepartmentAsync_WithNameExceeding100Chars_ThrowsValidationException()
        {
            // ARRANGE — name that is 101 characters (exceeds max 100)
            var invalidDto = TestDataBuilder.CreateDepartmentDto(name: new string('A', 101));

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateDepartmentAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateDepartmentAsync_WithDescriptionExceeding500Chars_ThrowsValidationException()
        {
            // ARRANGE — description that is 501 characters (exceeds max 500)
            var invalidDto = TestDataBuilder.CreateDepartmentDto(
                name: "Cardiology",
                description: new string('X', 501));

            // ACT + ASSERT
            await _sut.Invoking(s => s.CreateDepartmentAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateDepartmentAsync_WithValidData_CallsAddAsyncOnce()
        {
            // ARRANGE
            var createDto = TestDataBuilder.CreateDepartmentDto();
            _mockDepartmentRepository
                .Setup(r => r.AddAsync(It.IsAny<Department>()))
                .ReturnsAsync((Department entity) => entity);

            // ACT
            await _sut.CreateDepartmentAsync(createDto);

            // ASSERT — verify the repository's AddAsync was called exactly once
            // This confirms the service actually persisted the entity.
            _mockDepartmentRepository.Verify(r =>
                r.AddAsync(It.IsAny<Department>()), Times.Once);
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE DEPARTMENT
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateDepartmentAsync_WhenDepartmentExists_UpdatesSuccessfully()
        {
            // ARRANGE
            var existingDepartment = TestDataBuilder.CreateDepartment();
            var updateDto = TestDataBuilder.UpdateDepartmentDto(id: existingDepartment.Id);

            // Mock: the department is found in the database
            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(existingDepartment.Id))
                .ReturnsAsync(existingDepartment);

            // ACT — UpdateDepartmentAsync returns void (Task), no result to inspect
            await _sut.UpdateDepartmentAsync(updateDto);

            // ASSERT — verify SaveChanges was committed
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateDepartmentAsync_WhenDepartmentDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE — simulate: department not found in database
            var updateDto = TestDataBuilder.UpdateDepartmentDto();
            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Department?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.UpdateDepartmentAsync(updateDto))
                .Should().ThrowAsync<NotFoundException>();

            // Verify nothing was saved — can't update what doesn't exist
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateDepartmentAsync_WithEmptyName_ThrowsValidationException()
        {
            // ARRANGE — empty name fails validation before any DB call
            var invalidDto = new UpdateDepartmentDto
            {
                Id = Guid.NewGuid(),
                Name = "",           // ← violates NotEmpty rule
                Description = "Valid description"
            };

            // ACT + ASSERT
            await _sut.Invoking(s => s.UpdateDepartmentAsync(invalidDto))
                .Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();

            // The repository's GetByIdAsync should NEVER be called — validation
            // runs BEFORE we touch the database. This is the correct order.
            _mockDepartmentRepository.Verify(r =>
                r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE DEPARTMENT
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteDepartmentAsync_WhenDepartmentExists_DeletesSuccessfully()
        {
            // ARRANGE
            var department = TestDataBuilder.CreateDepartment();
            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(department.Id))
                .ReturnsAsync(department);

            // ACT
            await _sut.DeleteDepartmentAsync(department.Id);

            // ASSERT
            // 1. DeleteAsync was called with the correct entity
            _mockDepartmentRepository.Verify(r =>
                r.DeleteAsync(department), Times.Once);

            // 2. SaveChanges was committed — without this, the soft-delete
            //    flag would never be written to the database
            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteDepartmentAsync_WhenDepartmentDoesNotExist_ThrowsNotFoundException()
        {
            // ARRANGE — simulate department not found
            var nonExistentId = Guid.NewGuid();
            _mockDepartmentRepository
                .Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Department?)null);

            // ACT + ASSERT
            await _sut.Invoking(s => s.DeleteDepartmentAsync(nonExistentId))
                .Should().ThrowAsync<NotFoundException>();

            // Verify DeleteAsync was never called — can't delete what doesn't exist.
            // Without this check, a service bug (calling delete on null) would
            // cause a NullReferenceException in production instead of a clean 404.
            _mockDepartmentRepository.Verify(r =>
                r.DeleteAsync(It.IsAny<Department>()), Times.Never);

            _mockUnitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
