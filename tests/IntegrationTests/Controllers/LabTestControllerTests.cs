using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    public class LabTestControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/labtest";

        public LabTestControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ════════════════════════════════════════════════════════════════════
        // GET ALL (PAGED)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_AsLabTechnician_Returns200WithPagedResponse()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}?pageNumber=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResponse<LabTestDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.PageNumber.Should().Be(1);
            body.Data.PageSize.Should().Be(10);
            body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAll_AsDoctor_Returns200WithPagedResponse()
        {
            // ARRANGE — Doctors can browse lab test catalog to order tests
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetAll_AsPatient_Returns403()
        {
            // ARRANGE — Patients cannot view internal test catalog directly
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetAll_Unauthenticated_Returns401()
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
        public async Task GetById_WhenExists_AsLabTechnician_Returns200()
        {
            // ARRANGE
            var techClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);
            var created = await CreateLabTestAsync(techClient);

            // ACT
            var response = await techClient.GetAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabTestDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().Be(created.Id);
            body.Data.Code.Should().Be(created.Code);
        }

        [Fact]
        public async Task GetById_WhenNotFound_Returns404()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_AsLabTechnician_Returns201()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);
            var dto = BuildCreateLabTestDto();

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabTestDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Code.Should().Be(dto.Code);
            body.Data.Name.Should().Be(dto.Name);
            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_AsDoctor_Returns403()
        {
            // ARRANGE — Doctors cannot create lab tests in catalog
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var dto = BuildCreateLabTestDto();

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_DuplicateCode_Returns400()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);
            var existing = await CreateLabTestAsync(client);

            var duplicateDto = BuildCreateLabTestDto();
            duplicateDto.Code = existing.Code;

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, duplicateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_InvalidModel_Returns400()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);
            var invalidDto = new CreateLabTestDto { Name = "" };

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, invalidDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_AsLabTechnician_Returns200()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);
            var created = await CreateLabTestAsync(client);

            var updateDto = new UpdateLabTestDto
            {
                Id = created.Id,
                Name = "Updated Lab Test Name",
                Code = created.Code,
                Category = created.Category,
                Description = "Updated description",
                Price = 65.00m,
                TurnaroundTimeHours = 8,
                SampleType = created.SampleType,
                ReferenceRange = created.ReferenceRange,
                Unit = created.Unit
            };

            // ACT
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify
            var verifyRes = await client.GetAsync($"{BaseUrl}/{created.Id}");
            var body = await verifyRes.Content
                .ReadFromJsonAsync<ApiResponse<LabTestDto>>(TestJsonOptions.Default);

            body!.Data!.Name.Should().Be("Updated Lab Test Name");
            body.Data.Price.Should().Be(65.00m);
        }

        [Fact]
        public async Task Update_MismatchedId_Returns400()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);
            var created = await CreateLabTestAsync(client);

            var updateDto = new UpdateLabTestDto
            {
                Id = Guid.NewGuid(),
                Name = "Mismatched",
                Code = created.Code,
                Category = created.Category,
                Price = 10m,
                TurnaroundTimeHours = 2,
                SampleType = "Blood"
            };

            // ACT
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_AsPatient_Returns403()
        {
            // ARRANGE
            var techClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);
            var created = await CreateLabTestAsync(techClient);

            var updateDto = new UpdateLabTestDto
            {
                Id = created.Id,
                Name = "Patient Attempt",
                Code = created.Code,
                Category = created.Category,
                Price = 10m,
                TurnaroundTimeHours = 2,
                SampleType = "Blood"
            };

            // ACT
            var response = await patientClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_AsAdmin_Returns200AndSoftDeletes()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateLabTestAsync(adminClient);

            // ACT
            var response = await adminClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var getRes = await adminClient.GetAsync($"{BaseUrl}/{created.Id}");
            getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_AsLabTechnician_Returns403()
        {
            // ARRANGE — Only Admin and SuperAdmin can soft-delete catalog tests
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var techClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);
            var created = await CreateLabTestAsync(adminClient);

            // ACT
            var response = await techClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        private static CreateLabTestDto BuildCreateLabTestDto() =>
            new()
            {
                Name = $"LabTest_{Guid.NewGuid():N}"[..12],
                Code = $"LT_{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
                Category = "Hematology",
                Description = "Routine blood count test",
                Price = 35.00m,
                TurnaroundTimeHours = 4,
                SampleType = "Whole Blood",
                ReferenceRange = "4.0 - 10.0 x10^3/uL",
                Unit = "x10^3/uL"
            };

        private static async Task<LabTestDto> CreateLabTestAsync(HttpClient client)
        {
            var response = await client.PostAsJsonAsync(BaseUrl, BuildCreateLabTestDto());
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabTestDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }
    }
}
