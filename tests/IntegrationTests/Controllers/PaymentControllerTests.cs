using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    public class PaymentControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/payment";
        private const string InvoiceBaseUrl = "/api/v1/invoice";

        public PaymentControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ════════════════════════════════════════════════════════════════════
        // GET ALL (PAGED)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_AsCashier_Returns200WithPagedResponse()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}?pageNumber=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResponse<PaymentDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.PageNumber.Should().Be(1);
            body.Data.PageSize.Should().Be(10);
            body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAll_AsPatient_Returns403()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // PROCESS PAYMENT & VERIFY INVOICE UPDATES
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task ProcessPayment_PartialPayment_AsCashier_TransitionsInvoiceToPartiallyPaid()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var cashierClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);

            var invoice = await CreateInvoiceAsync(adminClient, cashierClient, totalExpected: 200m);

            var paymentPayload = new ProcessPaymentDto
            {
                InvoiceId = invoice.Id,
                Amount = 80m,
                Method = PaymentMethod.Cash,
                Notes = "Part payment received"
            };

            // ACT
            var response = await cashierClient.PostAsJsonAsync(BaseUrl, paymentPayload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var paymentBody = await response.Content
                .ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default);

            paymentBody!.Success.Should().BeTrue();
            paymentBody.Data!.Amount.Should().Be(80m);
            paymentBody.Data.Status.Should().Be(PaymentStatus.Success.ToString());

            // Check Invoice status
            var invRes = await cashierClient.GetAsync($"{InvoiceBaseUrl}/{invoice.Id}");
            var invBody = await invRes.Content
                .ReadFromJsonAsync<ApiResponse<InvoiceDto>>(TestJsonOptions.Default);

            invBody!.Data!.PaidAmount.Should().Be(80m);
            invBody.Data.Status.Should().Be(InvoiceStatus.PartiallyPaid.ToString());
            invBody.Data.BalanceDue.Should().Be(120m);
        }

        [Fact]
        public async Task ProcessPayment_FullPayment_AsCashier_TransitionsInvoiceToPaid()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var cashierClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);

            var invoice = await CreateInvoiceAsync(adminClient, cashierClient, totalExpected: 150m);

            var paymentPayload = new ProcessPaymentDto
            {
                InvoiceId = invoice.Id,
                Amount = 150m,
                Method = PaymentMethod.CreditCard,
                TransactionReference = "CARD-AUTH-9999",
                Notes = "Full settlement via VISA"
            };

            // ACT
            var response = await cashierClient.PostAsJsonAsync(BaseUrl, paymentPayload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var invRes = await cashierClient.GetAsync($"{InvoiceBaseUrl}/{invoice.Id}");
            var invBody = await invRes.Content
                .ReadFromJsonAsync<ApiResponse<InvoiceDto>>(TestJsonOptions.Default);

            invBody!.Data!.PaidAmount.Should().Be(150m);
            invBody.Data.Status.Should().Be(InvoiceStatus.Paid.ToString());
            invBody.Data.BalanceDue.Should().Be(0m);
        }

        [Fact]
        public async Task ProcessPayment_AsPatient_Returns403()
        {
            // ARRANGE — Patients do not have access to process counter payments directly
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var cashierClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var invoice = await CreateInvoiceAsync(adminClient, cashierClient, totalExpected: 100m);

            var paymentPayload = new ProcessPaymentDto
            {
                InvoiceId = invoice.Id,
                Amount = 50m
            };

            // ACT
            var response = await patientClient.PostAsJsonAsync(BaseUrl, paymentPayload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetByInvoiceId_WhenExists_AsPatient_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var cashierClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Cashier);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var invoice = await CreateInvoiceAsync(adminClient, cashierClient, totalExpected: 100m);

            await cashierClient.PostAsJsonAsync(BaseUrl, new ProcessPaymentDto
            {
                InvoiceId = invoice.Id,
                Amount = 50m,
                Method = PaymentMethod.Cash
            });

            // ACT
            var response = await patientClient.GetAsync($"{BaseUrl}/invoice/{invoice.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<IReadOnlyList<PaymentDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().ContainSingle(p => p.Amount == 50m);
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        private static async Task<PatientDto> CreatePatientAsync(HttpClient adminClient)
        {
            var payload = new CreatePatientDto
            {
                FirstName = "Pay",
                LastName = $"Patient_{Guid.NewGuid():N}"[..8],
                DateOfBirth = DateTime.UtcNow.AddYears(-28),
                Gender = Gender.Male,
                BloodGroup = BloodGroup.APositive,
                ContactNumber = "+1234567890",
                Address = "789 Payment Rd",
                EmergencyContactName = "Jane Doe",
                EmergencyContactNumber = "+1987654321"
            };

            var response = await adminClient.PostAsJsonAsync("/api/v1/patient", payload);
            response.EnsureSuccessStatusCode();
            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PatientDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }

        private static async Task<InvoiceDto> CreateInvoiceAsync(
            HttpClient adminClient,
            HttpClient cashierClient,
            decimal totalExpected)
        {
            var patient = await CreatePatientAsync(adminClient);

            var payload = new CreateInvoiceDto
            {
                PatientId = patient.Id,
                TaxPercentage = 0m,
                DiscountAmount = 0m,
                Items = new List<CreateInvoiceItemDto>
                {
                    new()
                    {
                        ItemType = BillingItemType.Consultation,
                        Description = "Medical Consultation",
                        UnitPrice = totalExpected,
                        Quantity = 1
                    }
                }
            };

            var response = await cashierClient.PostAsJsonAsync(InvoiceBaseUrl, payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<InvoiceDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }
    }
}
