using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    public class MedicationControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/medication";

        public MedicationControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ════════════════════════════════════════════════════════════════════
        // GET ALL (PAGED)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_AsPharmacist_Returns200WithPagedResponse()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}?pageNumber=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResponse<MedicationDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.PageNumber.Should().Be(1);
            body.Data.PageSize.Should().Be(10);
            body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAll_AsDoctor_Returns200WithPagedResponse()
        {
            // ARRANGE — Doctors can browse the medication catalog when prescribing
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetAll_AsPatient_Returns403()
        {
            // ARRANGE — Patients do not have access to backend medication inventory
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await client.GetAsync(BaseUrl);

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
        public async Task GetById_WhenExists_Returns200WithMedicationDto()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateMedicationAsync(adminClient);

            var pharmacistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);

            // ACT
            var response = await pharmacistClient.GetAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<MedicationDto>>(TestJsonOptions.Default);

            body!.Data!.Id.Should().Be(created.Id);
            body.Data.Name.Should().Be(created.Name);
            body.Data.Price.Should().Be(created.Price);
        }

        [Fact]
        public async Task GetById_WhenDoesNotExist_Returns404()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // GET LOW STOCK
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetLowStock_AsPharmacist_Returns200WithList()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/low-stock?threshold=50");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<IReadOnlyList<MedicationDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetLowStock_AsDoctor_Returns403()
        {
            // ARRANGE — Inventory low stock alerts are for pharmacy and admin staff only
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/low-stock");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_AsPharmacist_Returns201WithCreatedMedication()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);
            var dto = BuildCreateMedicationDto();

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<MedicationDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().NotBeEmpty();
            body.Data.Name.Should().Be(dto.Name);
            body.Data.StockQuantity.Should().Be(dto.StockQuantity);

            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_AsDoctor_Returns403()
        {
            // ARRANGE — Doctors cannot add medications to inventory
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var dto = BuildCreateMedicationDto();

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WithEmptyName_Returns400()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);
            var dto = BuildCreateMedicationDto();
            dto.Name = ""; // invalid

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
            var dto = BuildCreateMedicationDto();

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_AsPharmacist_WhenExists_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var pharmacistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);

            var created = await CreateMedicationAsync(adminClient);

            var updateDto = new UpdateMedicationDto
            {
                Id = created.Id,
                Name = $"{created.Name} Updated",
                GenericName = created.GenericName,
                Category = created.Category,
                DosageForm = created.DosageForm,
                Strength = created.Strength,
                Price = 25.00m,
                StockQuantity = 200,
                Manufacturer = created.Manufacturer,
                RequiresPrescription = created.RequiresPrescription
            };

            // ACT
            var response = await pharmacistClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify persistence
            var getResponse = await pharmacistClient.GetAsync($"{BaseUrl}/{created.Id}");
            var body = await getResponse.Content
                .ReadFromJsonAsync<ApiResponse<MedicationDto>>(TestJsonOptions.Default);

            body!.Data!.Name.Should().Be($"{created.Name} Updated");
            body.Data.StockQuantity.Should().Be(200);
            body.Data.Price.Should().Be(25.00m);
        }

        [Fact]
        public async Task Update_WhenIdMismatch_Returns400()
        {
            // ARRANGE
            var pharmacistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);
            var updateDto = new UpdateMedicationDto
            {
                Id = Guid.NewGuid(), // different from URL
                Name = "Mismatch",
                GenericName = "Mismatch Generic",
                Category = "Analgesics",
                DosageForm = "Tablet",
                Strength = "500mg",
                Price = 10m,
                StockQuantity = 50
            };

            // ACT
            var response = await pharmacistClient.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_WhenDoesNotExist_Returns404()
        {
            // ARRANGE
            var pharmacistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);
            var nonExistentId = Guid.NewGuid();
            var updateDto = new UpdateMedicationDto
            {
                Id = nonExistentId,
                Name = "Not Found",
                GenericName = "Generic",
                Category = "Analgesics",
                DosageForm = "Tablet",
                Strength = "500mg",
                Price = 10m,
                StockQuantity = 50
            };

            // ACT
            var response = await pharmacistClient.PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_AsAdmin_WhenExists_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateMedicationAsync(adminClient);

            // ACT
            var response = await adminClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Soft-delete verification: subsequent get returns 404
            var getResponse = await adminClient.GetAsync($"{BaseUrl}/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
                because: "soft-deleted medications must be excluded by global query filter");
        }

        [Fact]
        public async Task Delete_AsPharmacist_Returns403()
        {
            // ARRANGE — Only Admin and above can delete medications from the database catalog
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var pharmacistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);
            var created = await CreateMedicationAsync(adminClient);

            // ACT
            var response = await pharmacistClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        private static CreateMedicationDto BuildCreateMedicationDto() =>
            new()
            {
                Name = $"Med_{Guid.NewGuid():N}"[..10],
                GenericName = "Paracetamol Generic",
                Category = "Analgesics",
                DosageForm = "Tablet",
                Strength = "500mg",
                Price = 8.50m,
                StockQuantity = 100,
                Manufacturer = "HealthCare Pharma",
                RequiresPrescription = false
            };

        private static async Task<MedicationDto> CreateMedicationAsync(HttpClient client)
        {
            var response = await client.PostAsJsonAsync(BaseUrl, BuildCreateMedicationDto());
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<MedicationDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }
    }
}
