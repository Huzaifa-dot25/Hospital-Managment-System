using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // DOCTORCONTROLLERTESTS
    //
    // Tests the full Doctor CRUD pipeline end-to-end including:
    //   - Role-based authorization (Admin can create/update/delete, clinical/reception roles cannot)
    //   - All authenticated roles (including Patient) can browse/view doctors
    //   - Eager loading verifies DepartmentName is populated on read
    //   - Foreign key business rule: referenced Department must exist (404 if missing)
    //   - 404 on missing doctors
    //   - Soft-delete prevents subsequent retrieval (404)
    //   - Validation errors return 400
    //   - Unauthenticated requests return 401
    //   - Wrong-role requests return 403
    // ─────────────────────────────────────────────────────────────────────────
    public class DoctorControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/doctor";

        public DoctorControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ════════════════════════════════════════════════════════════════════
        // GET ALL (PAGED)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_AsDoctor_Returns200WithPagedResponse()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}?pageNumber=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResponse<DoctorDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.PageNumber.Should().Be(1);
            body.Data.PageSize.Should().Be(10);
            body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAll_AsPatient_Returns200WithPagedResponse()
        {
            // ARRANGE — Patients are permitted to browse doctors when scheduling appointments
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResponse<DoctorDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAll_WithoutToken_Returns401()
        {
            // ARRANGE
            var client = _factory.CreateUnauthenticatedClient();

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // GET BY ID
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetById_WhenDoctorExists_Returns200WithDoctorDtoAndDepartmentName()
        {
            // ARRANGE — create doctor via admin client
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDoctorAsync(adminClient);

            // ACT — read as patient (or any authorized role)
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);
            var response = await patientClient.GetAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<DoctorDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().Be(created.Id);
            body.Data.FirstName.Should().Be(created.FirstName);
            body.Data.LastName.Should().Be(created.LastName);
            body.Data.Specialization.Should().Be(created.Specialization);
            body.Data.DepartmentId.Should().Be(created.DepartmentId);

            // Verify eager-loading populated DepartmentName
            body.Data.DepartmentName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetById_WhenDoctorDoesNotExist_Returns404()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_WithoutToken_Returns401()
        {
            // ARRANGE
            var client = _factory.CreateUnauthenticatedClient();

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_AsAdmin_Returns201WithCreatedDoctor()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var department = await CreateDepartmentAsync(adminClient);
            var dto = BuildCreateDoctorDto(department.Id);

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<DoctorDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().NotBeEmpty();
            body.Data.FirstName.Should().Be(dto.FirstName);
            body.Data.LastName.Should().Be(dto.LastName);
            body.Data.Specialization.Should().Be(dto.Specialization);
            body.Data.LicenseNumber.Should().Be(dto.LicenseNumber);
            body.Data.DepartmentId.Should().Be(department.Id);
            body.Data.DepartmentName.Should().Be(department.Name);

            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_AsDoctor_Returns403()
        {
            // ARRANGE — Doctors cannot register other doctors (Admin only)
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var department = await CreateDepartmentAsync(adminClient);
            var dto = BuildCreateDoctorDto(department.Id);

            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await doctorClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_AsReceptionist_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var department = await CreateDepartmentAsync(adminClient);
            var dto = BuildCreateDoctorDto(department.Id);

            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            // ACT
            var response = await receptionistClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_AsPatient_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var department = await CreateDepartmentAsync(adminClient);
            var dto = BuildCreateDoctorDto(department.Id);

            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await patientClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WithoutToken_Returns401()
        {
            // ARRANGE
            var client = _factory.CreateUnauthenticatedClient();
            var dto = BuildCreateDoctorDto(Guid.NewGuid());

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_WhenDepartmentDoesNotExist_Returns404()
        {
            // ARRANGE — Foreign key constraint check at service layer
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var nonExistentDeptId = Guid.NewGuid();
            var dto = BuildCreateDoctorDto(nonExistentDeptId);

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT — NotFoundException for Department returns 404
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WithEmptyFirstName_Returns400()
        {
            // ARRANGE — FluentValidation rule failure
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var department = await CreateDepartmentAsync(adminClient);
            var dto = BuildCreateDoctorDto(department.Id);
            dto.FirstName = "";

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WithInvalidContactNumber_Returns400()
        {
            // ARRANGE — fails regex ^\+?[1-9]\d{1,14}$
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var department = await CreateDepartmentAsync(adminClient);
            var dto = BuildCreateDoctorDto(department.Id);
            dto.ContactNumber = "invalid-phone";

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WithNegativeExperience_Returns400()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var department = await CreateDepartmentAsync(adminClient);
            var dto = BuildCreateDoctorDto(department.Id);
            dto.YearsOfExperience = -1;

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_AsAdmin_WhenDoctorExists_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDoctorAsync(adminClient);

            var updateDto = new UpdateDoctorDto
            {
                Id = created.Id,
                FirstName = "Gregory",
                LastName = "House (Updated)",
                Specialization = "Chief of Diagnostic Medicine",
                LicenseNumber = created.LicenseNumber,
                YearsOfExperience = 20,
                ContactNumber = "+1987654321",
                DepartmentId = created.DepartmentId
            };

            // ACT
            var response = await adminClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify persistence
            var getResponse = await adminClient.GetAsync($"{BaseUrl}/{created.Id}");
            var body = await getResponse.Content
                .ReadFromJsonAsync<ApiResponse<DoctorDto>>(TestJsonOptions.Default);

            body!.Data!.LastName.Should().Be("House (Updated)");
            body.Data.Specialization.Should().Be("Chief of Diagnostic Medicine");
            body.Data.YearsOfExperience.Should().Be(20);
        }

        [Fact]
        public async Task Update_WhenIdMismatch_Returns400()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDoctorAsync(adminClient);

            var updateDto = new UpdateDoctorDto
            {
                Id = Guid.NewGuid(), // different from URL
                FirstName = "Mismatch",
                LastName = "Test",
                Specialization = "General",
                LicenseNumber = "LIC-999",
                YearsOfExperience = 5,
                ContactNumber = "+1234567890",
                DepartmentId = created.DepartmentId
            };

            // ACT
            var response = await adminClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_WhenDoctorDoesNotExist_Returns404()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var department = await CreateDepartmentAsync(adminClient);
            var nonExistentId = Guid.NewGuid();

            var updateDto = new UpdateDoctorDto
            {
                Id = nonExistentId,
                FirstName = "Not",
                LastName = "Found",
                Specialization = "General",
                LicenseNumber = "LIC-404",
                YearsOfExperience = 5,
                ContactNumber = "+1234567890",
                DepartmentId = department.Id
            };

            // ACT
            var response = await adminClient.PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_AsDoctor_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDoctorAsync(adminClient);

            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var updateDto = new UpdateDoctorDto
            {
                Id = created.Id,
                FirstName = created.FirstName,
                LastName = created.LastName,
                Specialization = "Surgery",
                LicenseNumber = created.LicenseNumber,
                YearsOfExperience = 10,
                ContactNumber = created.ContactNumber,
                DepartmentId = created.DepartmentId
            };

            // ACT
            var response = await doctorClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Update_WithoutToken_Returns401()
        {
            // ARRANGE
            var client = _factory.CreateUnauthenticatedClient();
            var dummyId = Guid.NewGuid();

            // ACT
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{dummyId}", new UpdateDoctorDto { Id = dummyId });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_AsAdmin_WhenDoctorExists_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDoctorAsync(adminClient);

            // ACT
            var response = await adminClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Soft-deleted doctor must not be returned on subsequent get
            var getResponse = await adminClient.GetAsync($"{BaseUrl}/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
                because: "soft-deleted doctor must be excluded by global query filter");
        }

        [Fact]
        public async Task Delete_AsDoctor_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDoctorAsync(adminClient);

            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await doctorClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_AsReceptionist_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDoctorAsync(adminClient);

            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            // ACT
            var response = await receptionistClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_WhenDoctorDoesNotExist_Returns404()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);

            // ACT
            var response = await adminClient.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WithoutToken_Returns401()
        {
            // ARRANGE
            var client = _factory.CreateUnauthenticatedClient();

            // ACT
            var response = await client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        private static CreateDoctorDto BuildCreateDoctorDto(Guid departmentId) =>
            new()
            {
                FirstName = $"Doc_{Guid.NewGuid():N}"[..8],
                LastName = "House",
                Specialization = "Diagnostic Medicine",
                LicenseNumber = $"LIC-{Guid.NewGuid():N}"[..12],
                YearsOfExperience = 12,
                ContactNumber = "+1234567890",
                DepartmentId = departmentId
            };

        private static async Task<DepartmentDto> CreateDepartmentAsync(HttpClient client, string? name = null)
        {
            var payload = new CreateDepartmentDto
            {
                Name = name ?? $"Dept_{Guid.NewGuid():N}"[..12],
                Description = "Department created for doctor test"
            };

            var response = await client.PostAsJsonAsync("/api/v1/department", payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<DepartmentDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }

        private static async Task<DoctorDto> CreateDoctorAsync(HttpClient adminClient, Guid? departmentId = null)
        {
            var deptId = departmentId ?? (await CreateDepartmentAsync(adminClient)).Id;
            var response = await adminClient.PostAsJsonAsync(BaseUrl, BuildCreateDoctorDto(deptId));
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<DoctorDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }
    }
}
