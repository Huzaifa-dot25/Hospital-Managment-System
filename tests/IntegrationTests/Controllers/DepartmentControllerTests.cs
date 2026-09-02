using Hospital.Shared.Constants;

namespace IntegrationTests.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // DEPARTMENTCONTROLLERTESTS
    //
    // Tests the full Department CRUD pipeline end-to-end including:
    //   - Role-based authorization (Admin can create, Doctor cannot)
    //   - 401 for unauthenticated requests
    //   - 403 for authenticated but wrong-role requests
    //   - 404 for non-existent resources
    //   - Correct response shape (ApiResponse<DepartmentDto>)
    //   - Validation errors return 400
    //
    // TEST ISOLATION STRATEGY:
    //   Each test that CREATES data uses a unique department name.
    //   Tests that READ/UPDATE/DELETE first create their own resource —
    //   they don't depend on data left behind by other tests.
    //   This makes every test independently runnable and order-independent.
    // ─────────────────────────────────────────────────────────────────────────
    public class DepartmentControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/department";

        public DepartmentControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ════════════════════════════════════════════════════════════════════
        // GET ALL
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_AsAuthenticatedUser_Returns200WithList()
        {
            // ARRANGE — any authenticated user can list departments
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<IEnumerable<DepartmentDto>>>();

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAll_WithoutToken_Returns401()
        {
            // ARRANGE — no auth header
            var client = _factory.CreateUnauthenticatedClient();

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT — JWT middleware blocks unauthenticated requests
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // GET BY ID
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetById_WhenDepartmentExists_Returns200WithDepartment()
        {
            // ARRANGE — create a department first, then fetch it by ID
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDepartmentAsync(adminClient, uniqueName: "GetById_Test");

            // ACT — fetch by the real ID returned from the create call
            var response = await adminClient.GetAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentDto>>();
            body!.Data!.Id.Should().Be(created.Id);
            body.Data.Name.Should().Be(created.Name);
        }

        [Fact]
        public async Task GetById_WhenDepartmentDoesNotExist_Returns404()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var nonExistentId = Guid.NewGuid();

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/{nonExistentId}");

            // ASSERT — ExceptionMiddleware converts NotFoundException → 404
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_AsAdmin_Returns201WithCreatedDepartment()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var dto = new CreateDepartmentDto
            {
                Name = $"Neurology_{Guid.NewGuid():N}",
                Description = "Brain and nervous system disorders"
            };

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            // Controller returns 201 Created (via CreatedAtAction) on success
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentDto>>();
            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().NotBeEmpty();
            body.Data.Name.Should().Be(dto.Name);

            // The Location header must point to the new resource's URL
            // e.g. /api/v1/department/some-guid
            response.Headers.Location.Should().NotBeNull(
                because: "201 Created responses must include a Location header");
        }

        [Fact]
        public async Task Create_AsDoctor_Returns403Forbidden()
        {
            // ARRANGE — Doctor does NOT have permission to create departments
            // [Authorize(Roles = Roles.AdminAndAbove)] means only Admin/SuperAdmin
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var dto = new CreateDepartmentDto
            {
                Name = $"ForbiddenDept_{Guid.NewGuid():N}",
                Description = "Should never be created"
            };

            // ACT
            var response = await doctorClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            // 403 Forbidden = "I know who you are, but you're not allowed to do this"
            // (as opposed to 401 = "I don't know who you are")
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_AsPatient_Returns403Forbidden()
        {
            // ARRANGE — Patients have zero access to department management
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);
            var dto = new CreateDepartmentDto
            {
                Name = $"PatientDept_{Guid.NewGuid():N}",
                Description = "Should not be created by patient"
            };

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
            var dto = new CreateDepartmentDto { Name = "AnonDept", Description = "Test" };

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_WithEmptyName_Returns400ValidationError()
        {
            // ARRANGE — empty name violates the validator
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var dto = new CreateDepartmentDto { Name = "", Description = "Valid description" };

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT — ExceptionMiddleware converts ValidationException → 400
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_AsSuperAdmin_Returns201()
        {
            // ARRANGE — SuperAdmin is also allowed to create departments
            var superAdminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.SuperAdmin);
            var dto = new CreateDepartmentDto
            {
                Name = $"SuperAdminDept_{Guid.NewGuid():N}",
                Description = "Created by SuperAdmin"
            };

            // ACT
            var response = await superAdminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_AsAdmin_WhenDepartmentExists_Returns200()
        {
            // ARRANGE — create a department, then update it
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDepartmentAsync(adminClient, uniqueName: "Update_Original");

            var updateDto = new UpdateDepartmentDto
            {
                Id = created.Id,
                Name = $"Update_Renamed_{Guid.NewGuid():N}",
                Description = "Updated description"
            };

            // ACT
            var response = await adminClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
            body!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Update_WhenIdMismatch_Returns400()
        {
            // ARRANGE — URL id and body id are different
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDepartmentAsync(adminClient, uniqueName: "IdMismatch_Test");

            var updateDto = new UpdateDepartmentDto
            {
                Id = Guid.NewGuid(),   // ← different from the URL id
                Name = "Mismatch",
                Description = "Test"
            };

            // ACT — URL id is created.Id but body id is a different Guid
            var response = await adminClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT — controller explicitly checks id == body.Id and returns 400
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_AsDoctor_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateDepartmentAsync(adminClient, uniqueName: "DoctorUpdate_Test");

            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var updateDto = new UpdateDepartmentDto
            {
                Id = created.Id,
                Name = $"DoctorAttempt_{Guid.NewGuid():N}",
                Description = "Should fail"
            };

            // ACT
            var response = await doctorClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_AsSuperAdmin_Returns200()
        {
            // ARRANGE — only SuperAdmin can delete departments
            var superAdminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.SuperAdmin);
            var created = await CreateDepartmentAsync(superAdminClient, uniqueName: "ToDelete_Test");

            // ACT
            var response = await superAdminClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Delete_AsAdmin_Returns403()
        {
            // ARRANGE — Admin is NOT allowed to delete departments (SuperAdmin only)
            var superAdminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.SuperAdmin);
            var created = await CreateDepartmentAsync(superAdminClient, uniqueName: "AdminDelete_Test");

            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);

            // ACT
            var response = await adminClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_WhenDepartmentDoesNotExist_Returns404()
        {
            // ARRANGE
            var superAdminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.SuperAdmin);

            // ACT
            var response = await superAdminClient.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates a department via the real API and returns the created DepartmentDto.
        /// Used by tests that need an existing department to test GET/UPDATE/DELETE.
        ///
        /// The uniqueName parameter prevents name collisions between tests.
        /// Using Guid suffix ensures uniqueness even when tests run in parallel.
        /// </summary>
        private static async Task<DepartmentDto> CreateDepartmentAsync(
            HttpClient client, string uniqueName)
        {
            var dto = new CreateDepartmentDto
            {
                Name = $"{uniqueName}_{Guid.NewGuid():N}",
                Description = "Test department"
            };

            var response = await client.PostAsJsonAsync("/api/v1/department", dto);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentDto>>();
            return body!.Data!;
        }
    }
}
