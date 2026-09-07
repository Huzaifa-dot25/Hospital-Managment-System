using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    public class InvoiceControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/invoice";

        public InvoiceControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ════════════════════════════════════════════════════════════════════
        // GET ALL (PAGED)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_AsAccountant_Returns200WithPagedResponse()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Accountant);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}?pageNumber=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResponse<InvoiceDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.PageNumber.Should().Be(1);
            body.Data.PageSize.Should().Be(10);
            body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAll_AsCashier_Returns200WithPagedResponse()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetAll_AsPatient_Returns403()
        {
            // ARRANGE — Patients cannot browse hospital-wide billing invoices
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
        public async Task GetById_WhenExists_AsPatient_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var cashierClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var invoice = await CreateInvoiceAsync(adminClient, cashierClient);

            // ACT
            var response = await patientClient.GetAsync($"{BaseUrl}/{invoice.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<InvoiceDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().Be(invoice.Id);
            body.Data.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetById_WhenNotFound_Returns404()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Accountant);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE INVOICE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_AsCashier_Returns201WithCalculatedTotals()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var cashierClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);

            var patient = await CreatePatientAsync(adminClient);

            var payload = new CreateInvoiceDto
            {
                PatientId = patient.Id,
                TaxPercentage = 5m,
                DiscountAmount = 10m,
                Notes = "Consultation and tests",
                Items = new List<CreateInvoiceItemDto>
                {
                    new()
                    {
                        ItemType = BillingItemType.Consultation,
                        Description = "Specialist Consultation",
                        UnitPrice = 100m,
                        Quantity = 1
                    },
                    new()
                    {
                        ItemType = BillingItemType.LabTest,
                        Description = "Blood Test",
                        UnitPrice = 50m,
                        Quantity = 2
                    }
                }
            };
            // SubTotal = 100 + 100 = 200
            // Tax = 200 * 0.05 = 10
            // Total = 200 + 10 - 10 = 200

            // ACT
            var response = await cashierClient.PostAsJsonAsync(BaseUrl, payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<InvoiceDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.SubTotal.Should().Be(200m);
            body.Data.TaxAmount.Should().Be(10m);
            body.Data.TotalAmount.Should().Be(200m);
            body.Data.Status.Should().Be(InvoiceStatus.Pending.ToString());
            body.Data.InvoiceNumber.Should().StartWith("INV-");
            body.Data.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Create_AsPatient_Returns403()
        {
            // ARRANGE — Patients cannot create invoices
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var patient = await CreatePatientAsync(adminClient);
            var payload = new CreateInvoiceDto
            {
                PatientId = patient.Id,
                Items = new List<CreateInvoiceItemDto>
                {
                    new() { Description = "Item", UnitPrice = 50m, Quantity = 1 }
                }
            };

            // ACT
            var response = await patientClient.PostAsJsonAsync(BaseUrl, payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WithEmptyItems_Returns400()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var cashierClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);

            var patient = await CreatePatientAsync(adminClient);
            var payload = new CreateInvoiceDto
            {
                PatientId = patient.Id,
                Items = new List<CreateInvoiceItemDto>() // Empty
            };

            // ACT
            var response = await cashierClient.PostAsJsonAsync(BaseUrl, payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════════
        // CANCEL INVOICE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Cancel_AsAccountant_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var accountantClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Accountant);

            var invoice = await CreateInvoiceAsync(adminClient, accountantClient);

            // ACT
            var response = await accountantClient.PostAsJsonAsync($"{BaseUrl}/{invoice.Id}/cancel", new { });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<InvoiceDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Status.Should().Be(InvoiceStatus.Cancelled.ToString());
        }

        [Fact]
        public async Task Cancel_AsPatient_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var cashierClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var invoice = await CreateInvoiceAsync(adminClient, cashierClient);

            // ACT
            var response = await patientClient.PostAsJsonAsync($"{BaseUrl}/{invoice.Id}/cancel", new { });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        private static async Task<PatientDto> CreatePatientAsync(HttpClient adminClient)
        {
            var payload = new CreatePatientDto
            {
                FirstName = "Billing",
                LastName = $"Patient_{Guid.NewGuid():N}"[..8],
                DateOfBirth = DateTime.UtcNow.AddYears(-35),
                Gender = Gender.Female,
                BloodGroup = BloodGroup.OPositive,
                ContactNumber = "+1234567890",
                Address = "456 Billing Ave",
                EmergencyContactName = "John Doe",
                EmergencyContactNumber = "+1987654321"
            };

            var response = await adminClient.PostAsJsonAsync("/api/v1/patient", payload);
            response.EnsureSuccessStatusCode();
            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PatientDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }

        private static async Task<InvoiceDto> CreateInvoiceAsync(HttpClient adminClient, HttpClient billingClient)
        {
            var patient = await CreatePatientAsync(adminClient);

            var payload = new CreateInvoiceDto
            {
                PatientId = patient.Id,
                TaxPercentage = 10m,
                DiscountAmount = 0m,
                Items = new List<CreateInvoiceItemDto>
                {
                    new()
                    {
                        ItemType = BillingItemType.Consultation,
                        Description = "Doctor Consultation",
                        UnitPrice = 150m,
                        Quantity = 1
                    }
                }
            };

            var response = await billingClient.PostAsJsonAsync(BaseUrl, payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<InvoiceDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }
    }
}
