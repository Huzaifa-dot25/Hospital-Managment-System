using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // PATIENTCONTROLLERTESTS
    //
    // Tests the full Patient CRUD pipeline end-to-end including:
    //   - Role-based access (Receptionist can create, Patient role cannot)
    //   - Pagination metadata is correct
    //   - 404 on missing patients
    //   - Validation errors return 400
    //   - Unauthenticated requests return 401
    //   - Wrong-role requests return 403
    //
    // Patient is the most sensitive entity — wrong access control here
    // is a real-world HIPAA / data privacy violation. Every role boundary
    // must be verified explicitly.
    // ─────────────────────────────────────────────────────────────────────────
    public class PatientControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/patient";

        public PatientControllerTests(CustomWebApplicationFactory factory)
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

            // ACT — default pagination: page 1, pageSize 10
            var response = await client.GetAsync($"{BaseUrl}?pageNumber=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResponse<PatientDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();

            // Pagination metadata must always be present, even if empty
            body.Data!.PageNumber.Should().Be(1);
            body.Data.PageSize.Should().Be(10);
            body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAll_AsPatientRole_Returns403()
        {
            // ARRANGE — a Patient user should NOT be able to browse all patients
            // This is a critical HIPAA-like access control rule
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await patientClient.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
        public async Task GetById_WhenPatientExists_Returns200WithPatientDto()
        {
            // ARRANGE — create a patient, then retrieve it
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var created = await CreatePatientAsync(receptionistClient);

            // ACT
            var response = await receptionistClient.GetAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDto>>(TestJsonOptions.Default);
            body!.Data!.Id.Should().Be(created.Id);
            body.Data.FirstName.Should().Be(created.FirstName);
            body.Data.LastName.Should().Be(created.LastName);

            // FullName is a computed property — verify it is constructed correctly
            body.Data.FullName.Should().Be($"{created.FirstName} {created.LastName}");
        }

        [Fact]
        public async Task GetById_WhenPatientDoesNotExist_Returns404()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_AsReceptionist_Returns201WithCreatedPatient()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var dto = BuildCreatePatientDto();

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDto>>(TestJsonOptions.Default);
            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().NotBeEmpty();
            body.Data.FirstName.Should().Be(dto.FirstName);
            body.Data.LastName.Should().Be(dto.LastName);

            // Age must be calculated (we provide DoB 25 years ago → Age = 25)
            body.Data.Age.Should().BeGreaterThan(0);

            // Location header must point to the new patient
            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_AsAdmin_Returns201()
        {
            // ARRANGE — Admin can also register patients
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, BuildCreatePatientDto());

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task Create_AsDoctor_Returns403()
        {
            // ARRANGE — Doctors are clinical staff, not front-desk staff
            // Only Receptionist/Admin/SuperAdmin can register patients
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await doctorClient.PostAsJsonAsync(BaseUrl, BuildCreatePatientDto());

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_AsPatientRole_Returns403()
        {
            // ARRANGE — patients cannot register other patients
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await patientClient.PostAsJsonAsync(BaseUrl, BuildCreatePatientDto());

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WithEmptyFirstName_Returns400()
        {
            // ARRANGE — violates NotEmpty() on FirstName
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var dto = BuildCreatePatientDto();
            dto.FirstName = "";

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT — ExceptionMiddleware converts ValidationException → 400
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WithFutureDateOfBirth_Returns400()
        {
            // ARRANGE — DateOfBirth in the future is biologically impossible
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var dto = BuildCreatePatientDto();
            dto.DateOfBirth = DateTime.UtcNow.AddDays(1);

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WithInvalidContactNumber_Returns400()
        {
            // ARRANGE — fails the phone regex ^\+?[1-9]\d{1,14}$
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var dto = BuildCreatePatientDto();
            dto.ContactNumber = "not-a-phone";

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WithoutToken_Returns401()
        {
            // ARRANGE
            var client = _factory.CreateUnauthenticatedClient();

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, BuildCreatePatientDto());

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_AsReceptionist_WhenPatientExists_Returns200()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var created = await CreatePatientAsync(client);

            var updateDto = new UpdatePatientDto
            {
                Id = created.Id,
                FirstName = "Updated",
                LastName = "Name",
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                Gender = Gender.Male,
                BloodGroup = BloodGroup.BPositive,
                ContactNumber = "+9876543210",
                Address = "456 Updated Street",
                EmergencyContactName = "Emergency Contact",
                EmergencyContactNumber = "+1122334455"
            };

            // ACT
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the data was actually updated by fetching the patient
            var getResponse = await client.GetAsync($"{BaseUrl}/{created.Id}");
            var body = await getResponse.Content.ReadFromJsonAsync<ApiResponse<PatientDto>>(TestJsonOptions.Default);
            body!.Data!.FirstName.Should().Be("Updated");
            body.Data.LastName.Should().Be("Name");
        }

        [Fact]
        public async Task Update_WhenIdMismatch_Returns400()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var created = await CreatePatientAsync(client);

            var updateDto = new UpdatePatientDto
            {
                Id = Guid.NewGuid(),  // ← different from URL id
                FirstName = "Mismatch",
                LastName = "Test",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                ContactNumber = "+9876543210",
                Address = "Test Address",
                EmergencyContactName = "Contact",
                EmergencyContactNumber = "+1122334455"
            };

            // ACT
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_AsDoctor_Returns403()
        {
            // ARRANGE — Doctors cannot update patient registration data
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var created = await CreatePatientAsync(receptionistClient);

            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var updateDto = new UpdatePatientDto
            {
                Id = created.Id,
                FirstName = "Doctor",
                LastName = "Attempt",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                ContactNumber = "+9876543210",
                Address = "Test Address",
                EmergencyContactName = "Contact",
                EmergencyContactNumber = "+1122334455"
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
        public async Task Delete_AsAdmin_WhenPatientExists_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreatePatientAsync(adminClient);

            // ACT
            var response = await adminClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Soft delete: the patient should now return 404 when fetched
            // (because the global query filter excludes IsDeleted=true records)
            var getResponse = await adminClient.GetAsync($"{BaseUrl}/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
                because: "soft-deleted patients must not be visible via the API");
        }

        [Fact]
        public async Task Delete_AsDoctor_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreatePatientAsync(adminClient);

            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await doctorClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_WhenPatientDoesNotExist_Returns404()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);

            // ACT
            var response = await adminClient.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds a valid CreatePatientDto with safe defaults that pass all validators.
        /// ContactNumber and EmergencyContactNumber start with non-zero digit after +
        /// to satisfy the regex ^\+?[1-9]\d{1,14}$.
        /// DateOfBirth is 25 years ago — safely in the past.
        /// </summary>
        private static CreatePatientDto BuildCreatePatientDto() =>
            new()
            {
                FirstName = $"Test_{Guid.NewGuid().ToString("N")[..6]}",
                LastName = "Patient",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                Gender = Gender.Female,
                BloodGroup = BloodGroup.APositive,
                ContactNumber = "+1234567890",
                Address = "123 Test Street, Cairo",
                EmergencyContactName = "Emergency Contact",
                EmergencyContactNumber = "+9876543210"
            };

        /// <summary>
        /// Creates a patient via the real API and returns the created PatientDto.
        /// Centralised here to keep tests DRY.
        /// </summary>
        private static async Task<PatientDto> CreatePatientAsync(HttpClient client)
        {
            var response = await client.PostAsJsonAsync("/api/v1/patient", BuildCreatePatientDto());
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDto>>(TestJsonOptions.Default);
            return body!.Data!;
        }
    }
}
