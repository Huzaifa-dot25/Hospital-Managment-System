using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    public class LabOrderControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/laborder";

        public LabOrderControllerTests(CustomWebApplicationFactory factory)
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
                .ReadFromJsonAsync<ApiResponse<PagedResponse<LabOrderDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.PageNumber.Should().Be(1);
            body.Data.PageSize.Should().Be(10);
            body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAll_AsDoctor_Returns200WithPagedResponse()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetAll_AsPatient_Returns403()
        {
            // ARRANGE — Patients cannot view all lab orders across the hospital
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
            var docClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var order = await CreateLabOrderAsync(adminClient, docClient);

            // ACT
            var response = await patientClient.GetAsync($"{BaseUrl}/{order.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabOrderDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().Be(order.Id);
            body.Data.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetById_WhenNotFound_Returns404()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            // ACT
            var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE LAB ORDER
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_AsDoctor_Returns201()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var docClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var test = await CreateLabTestAsync(adminClient);

            var payload = new CreateLabOrderDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Priority = LabOrderPriority.Routine,
                ClinicalNotes = "Check liver function before surgery",
                Items = new List<CreateLabOrderItemDto>
                {
                    new() { LabTestId = test.Id }
                }
            };

            // ACT
            var response = await docClient.PostAsJsonAsync(BaseUrl, payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabOrderDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Status.Should().Be(LabOrderStatus.Ordered.ToString());
            body.Data.Items.Should().ContainSingle(i => i.LabTestId == test.Id);
            body.Data.TotalAmount.Should().Be(test.Price);
        }

        [Fact]
        public async Task Create_AsPatient_Returns403()
        {
            // ARRANGE — Patients cannot self-order lab tests
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var test = await CreateLabTestAsync(adminClient);

            var payload = new CreateLabOrderDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Items = new List<CreateLabOrderItemDto> { new() { LabTestId = test.Id } }
            };

            // ACT
            var response = await patientClient.PostAsJsonAsync(BaseUrl, payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // COLLECT SAMPLE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CollectSample_AsNurse_TransitionsToSampleCollected_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var docClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var nurseClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Nurse);

            var order = await CreateLabOrderAsync(adminClient, docClient);

            var collectPayload = new CollectSampleDto
            {
                SampleCollectedBy = "Nurse Emily",
                SampleCollectionDate = DateTime.UtcNow
            };

            // ACT
            var response = await nurseClient.PostAsJsonAsync($"{BaseUrl}/{order.Id}/collect-sample", collectPayload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabOrderDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Status.Should().Be(LabOrderStatus.SampleCollected.ToString());
            body.Data.SampleCollectedBy.Should().Be("Nurse Emily");
        }

        [Fact]
        public async Task CollectSample_AsPatient_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var docClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var order = await CreateLabOrderAsync(adminClient, docClient);

            // ACT
            var response = await patientClient.PostAsJsonAsync($"{BaseUrl}/{order.Id}/collect-sample", new CollectSampleDto());

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // RECORD RESULTS
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task RecordResults_AsLabTechnician_TransitionsToCompleted_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var docClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var nurseClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Nurse);
            var techClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.LabTechnician);

            var order = await CreateLabOrderAsync(adminClient, docClient);

            // Nurse collects sample
            await nurseClient.PostAsJsonAsync(
                $"{BaseUrl}/{order.Id}/collect-sample",
                new CollectSampleDto { SampleCollectedBy = "Nurse Emily" });

            var itemId = order.Items[0].Id;
            var resultsPayload = new RecordLabResultsDto
            {
                Results = new List<RecordLabItemResultDto>
                {
                    new()
                    {
                        LabItemId = itemId,
                        ResultValue = "13.8",
                        Unit = "g/dL",
                        ReferenceRange = "12.0 - 16.0 g/dL",
                        IsAbnormal = false,
                        Remarks = "Hemoglobin within normal range"
                    }
                }
            };

            // ACT
            var response = await techClient.PostAsJsonAsync($"{BaseUrl}/{order.Id}/results", resultsPayload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabOrderDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Status.Should().Be(LabOrderStatus.Completed.ToString());
            body.Data.CompletedDate.Should().NotBeNull();
            body.Data.Items[0].ResultValue.Should().Be("13.8");
            body.Data.Items[0].IsAbnormal.Should().BeFalse();
        }

        [Fact]
        public async Task RecordResults_AsDoctor_Returns403()
        {
            // ARRANGE — Doctors order tests, only Lab Technicians or Admins enter results
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var docClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var order = await CreateLabOrderAsync(adminClient, docClient);

            var resultsPayload = new RecordLabResultsDto
            {
                Results = new List<RecordLabItemResultDto>
                {
                    new() { LabItemId = order.Items[0].Id, ResultValue = "10" }
                }
            };

            // ACT
            var response = await docClient.PostAsJsonAsync($"{BaseUrl}/{order.Id}/results", resultsPayload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ════════════════════════════════════════════════════════════════════
        // CANCEL
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Cancel_AsDoctor_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var docClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var order = await CreateLabOrderAsync(adminClient, docClient);

            // ACT
            var response = await docClient.PostAsJsonAsync($"{BaseUrl}/{order.Id}/cancel", new { });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabOrderDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Status.Should().Be(LabOrderStatus.Cancelled.ToString());
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        private static async Task<PatientDto> CreatePatientAsync(HttpClient adminClient)
        {
            var payload = new CreatePatientDto
            {
                FirstName = "Lab",
                LastName = $"Patient_{Guid.NewGuid():N}"[..8],
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                Gender = Gender.Male,
                BloodGroup = BloodGroup.BPositive,
                ContactNumber = "+1234567890",
                Address = "123 Lab Street",
                EmergencyContactName = "Jane Doe",
                EmergencyContactNumber = "+1987654321"
            };

            var response = await adminClient.PostAsJsonAsync("/api/v1/patient", payload);
            response.EnsureSuccessStatusCode();
            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PatientDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }

        private static async Task<DoctorDto> CreateDoctorAsync(HttpClient adminClient)
        {
            var deptPayload = new CreateDepartmentDto
            {
                Name = $"Pathology_{Guid.NewGuid():N}"[..12],
                Description = "Pathology and Laboratory Department"
            };

            var deptResponse = await adminClient.PostAsJsonAsync("/api/v1/department", deptPayload);
            deptResponse.EnsureSuccessStatusCode();
            var deptBody = await deptResponse.Content
                .ReadFromJsonAsync<ApiResponse<DepartmentDto>>(TestJsonOptions.Default);

            var docPayload = new CreateDoctorDto
            {
                FirstName = "Lab",
                LastName = $"Doctor_{Guid.NewGuid():N}"[..8],
                Specialization = "Pathology",
                LicenseNumber = $"DOC-{Guid.NewGuid():N}"[..10],
                YearsOfExperience = 8,
                ContactNumber = "+1234567890",
                DepartmentId = deptBody!.Data!.Id
            };

            var docResponse = await adminClient.PostAsJsonAsync("/api/v1/doctor", docPayload);
            docResponse.EnsureSuccessStatusCode();
            var docBody = await docResponse.Content
                .ReadFromJsonAsync<ApiResponse<DoctorDto>>(TestJsonOptions.Default);

            return docBody!.Data!;
        }

        private static async Task<LabTestDto> CreateLabTestAsync(HttpClient adminClient)
        {
            var payload = new CreateLabTestDto
            {
                Name = $"Test_{Guid.NewGuid():N}"[..10],
                Code = $"LT_{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
                Category = "Biochemistry",
                Description = "Blood glucose fasting",
                Price = 25.00m,
                TurnaroundTimeHours = 2,
                SampleType = "Plasma",
                ReferenceRange = "70 - 99 mg/dL",
                Unit = "mg/dL"
            };

            var response = await adminClient.PostAsJsonAsync("/api/v1/labtest", payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabTestDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }

        private static async Task<LabOrderDto> CreateLabOrderAsync(HttpClient adminClient, HttpClient doctorClient)
        {
            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var test = await CreateLabTestAsync(adminClient);

            var payload = new CreateLabOrderDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Priority = LabOrderPriority.Routine,
                ClinicalNotes = "Routine lab check",
                Items = new List<CreateLabOrderItemDto>
                {
                    new() { LabTestId = test.Id }
                }
            };

            var response = await doctorClient.PostAsJsonAsync(BaseUrl, payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<LabOrderDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }
    }
}
