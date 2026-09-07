using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    public class PrescriptionControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/prescription";

        public PrescriptionControllerTests(CustomWebApplicationFactory factory)
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
                .ReadFromJsonAsync<ApiResponse<PagedResponse<PrescriptionDto>>>(TestJsonOptions.Default);

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
            // ARRANGE — Patients cannot browse hospital-wide prescriptions
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
        public async Task GetById_WhenExists_Returns200WithFullDetails()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var created = await CreatePrescriptionAsync(adminClient, doctorClient);

            // ACT — Fetch as pharmacist
            var pharmacistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);
            var response = await pharmacistClient.GetAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PrescriptionDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().Be(created.Id);
            body.Data.PatientName.Should().NotBeNullOrWhiteSpace();
            body.Data.DoctorName.Should().NotBeNullOrWhiteSpace();
            body.Data.Items.Should().NotBeEmpty();
            body.Data.Items[0].MedicationName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetById_AsPatient_Returns200()
        {
            // ARRANGE — Patients can read their prescription by ID
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var created = await CreatePrescriptionAsync(adminClient, doctorClient);

            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await patientClient.GetAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
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
        // CREATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_AsDoctor_Returns201WithCreatedPrescription()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var medication = await CreateMedicationAsync(adminClient);

            var dto = new CreatePrescriptionDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Notes = "Take 1 capsule every 8 hours with meals",
                Items = new List<CreatePrescriptionItemDto>
                {
                    new()
                    {
                        MedicationId = medication.Id,
                        Dosage = "500mg",
                        Frequency = "Three times daily",
                        DurationInDays = 7,
                        Quantity = 21,
                        Instructions = "After meals"
                    }
                }
            };

            // ACT
            var response = await doctorClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PrescriptionDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().NotBeEmpty();
            body.Data.PatientId.Should().Be(patient.Id);
            body.Data.DoctorId.Should().Be(doctor.Id);
            body.Data.Status.Should().Be(PrescriptionStatus.Pending);
            body.Data.Items.Should().HaveCount(1);
            body.Data.Items[0].Quantity.Should().Be(21);

            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_AsPharmacist_Returns403()
        {
            // ARRANGE — Pharmacists dispense, doctors create prescriptions
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var pharmacistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);

            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var medication = await CreateMedicationAsync(adminClient);

            var dto = new CreatePrescriptionDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Items = new List<CreatePrescriptionItemDto>
                {
                    new()
                    {
                        MedicationId = medication.Id,
                        Dosage = "500mg",
                        Frequency = "Twice daily",
                        DurationInDays = 5,
                        Quantity = 10
                    }
                }
            };

            // ACT
            var response = await pharmacistClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WithEmptyItems_Returns400()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = new CreatePrescriptionDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Items = new List<CreatePrescriptionItemDto>() // empty!
            };

            // ACT
            var response = await doctorClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WhenMedicationDoesNotExist_Returns404()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = new CreatePrescriptionDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Items = new List<CreatePrescriptionItemDto>
                {
                    new()
                    {
                        MedicationId = Guid.NewGuid(), // does not exist
                        Dosage = "500mg",
                        Frequency = "Daily",
                        DurationInDays = 5,
                        Quantity = 5
                    }
                }
            };

            // ACT
            var response = await doctorClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ════════════════════════════════════════════════════════════════════
        // DISPENSE WORKFLOW
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Dispense_AsPharmacist_UpdatesStatusAndDeductsStock()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var pharmacistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);

            var medication = await CreateMedicationAsync(adminClient, initialStock: 50);
            var prescription = await CreatePrescriptionAsync(adminClient, doctorClient, medication.Id, quantity: 15);

            var dispenseDto = new DispensePrescriptionDto
            {
                PharmacistId = Guid.NewGuid(),
                DispensingNotes = "Dispensed in full by Pharmacist"
            };

            // ACT
            var response = await pharmacistClient.PostAsJsonAsync(
                $"{BaseUrl}/{prescription.Id}/dispense", dispenseDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PrescriptionDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Status.Should().Be(PrescriptionStatus.Dispensed);
            body.Data.DispensedDate.Should().NotBeNull();

            // Verify medication stock was decremented from 50 to 35
            var medResponse = await pharmacistClient.GetAsync($"/api/v1/medication/{medication.Id}");
            var medBody = await medResponse.Content
                .ReadFromJsonAsync<ApiResponse<MedicationDto>>(TestJsonOptions.Default);

            medBody!.Data!.StockQuantity.Should().Be(35);
        }

        [Fact]
        public async Task Dispense_AsDoctor_Returns403()
        {
            // ARRANGE — Doctors cannot dispense medications
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var prescription = await CreatePrescriptionAsync(adminClient, doctorClient);

            // ACT
            var response = await doctorClient.PostAsJsonAsync(
                $"{BaseUrl}/{prescription.Id}/dispense",
                new DispensePrescriptionDto { PharmacistId = Guid.NewGuid() });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Dispense_WhenAlreadyDispensed_Returns400()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var pharmacistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Pharmacist);

            var medication = await CreateMedicationAsync(adminClient, initialStock: 50);
            var prescription = await CreatePrescriptionAsync(adminClient, doctorClient, medication.Id, quantity: 10);

            // First dispense succeeds
            await pharmacistClient.PostAsJsonAsync(
                $"{BaseUrl}/{prescription.Id}/dispense",
                new DispensePrescriptionDto { PharmacistId = Guid.NewGuid() });

            // ACT — Attempt second dispense
            var secondResponse = await pharmacistClient.PostAsJsonAsync(
                $"{BaseUrl}/{prescription.Id}/dispense",
                new DispensePrescriptionDto { PharmacistId = Guid.NewGuid() });

            // ASSERT
            secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════════
        // CANCEL
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Cancel_AsDoctor_WhenPending_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var prescription = await CreatePrescriptionAsync(adminClient, doctorClient);

            // ACT
            var response = await doctorClient.DeleteAsync($"{BaseUrl}/{prescription.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify status is Cancelled
            var getResponse = await doctorClient.GetAsync($"{BaseUrl}/{prescription.Id}");
            var body = await getResponse.Content
                .ReadFromJsonAsync<ApiResponse<PrescriptionDto>>(TestJsonOptions.Default);

            body!.Data!.Status.Should().Be(PrescriptionStatus.Cancelled);
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        private static async Task<PatientDto> CreatePatientAsync(HttpClient client)
        {
            var payload = new CreatePatientDto
            {
                FirstName = $"Pat_{Guid.NewGuid():N}"[..8],
                LastName = "PrescriptionTest",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                Gender = Gender.Male,
                BloodGroup = BloodGroup.BPositive,
                ContactNumber = "+1234567890",
                Address = "Prescription Address",
                EmergencyContactName = "Contact",
                EmergencyContactNumber = "+1987654321"
            };

            var response = await client.PostAsJsonAsync("/api/v1/patient", payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PatientDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }

        private static async Task<DoctorDto> CreateDoctorAsync(HttpClient adminClient)
        {
            var deptPayload = new CreateDepartmentDto
            {
                Name = $"Dept_{Guid.NewGuid():N}"[..12],
                Description = "Department for prescription test"
            };
            var deptResponse = await adminClient.PostAsJsonAsync("/api/v1/department", deptPayload);
            deptResponse.EnsureSuccessStatusCode();
            var deptBody = await deptResponse.Content
                .ReadFromJsonAsync<ApiResponse<DepartmentDto>>(TestJsonOptions.Default);

            var docPayload = new CreateDoctorDto
            {
                FirstName = $"Doc_{Guid.NewGuid():N}"[..8],
                LastName = "Watson",
                Specialization = "General Medicine",
                LicenseNumber = $"LIC-{Guid.NewGuid():N}"[..12],
                YearsOfExperience = 10,
                ContactNumber = "+1234567890",
                DepartmentId = deptBody!.Data!.Id
            };

            var docResponse = await adminClient.PostAsJsonAsync("/api/v1/doctor", docPayload);
            docResponse.EnsureSuccessStatusCode();
            var docBody = await docResponse.Content
                .ReadFromJsonAsync<ApiResponse<DoctorDto>>(TestJsonOptions.Default);

            return docBody!.Data!;
        }

        private static async Task<MedicationDto> CreateMedicationAsync(HttpClient adminClient, int initialStock = 100)
        {
            var payload = new CreateMedicationDto
            {
                Name = $"Med_{Guid.NewGuid():N}"[..10],
                GenericName = "Generic Medication",
                Category = "Antibiotics",
                DosageForm = "Capsule",
                Strength = "500mg",
                Price = 15.00m,
                StockQuantity = initialStock,
                RequiresPrescription = true
            };

            var response = await adminClient.PostAsJsonAsync("/api/v1/medication", payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<MedicationDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }

        private static async Task<PrescriptionDto> CreatePrescriptionAsync(
            HttpClient adminClient,
            HttpClient doctorClient,
            Guid? medicationId = null,
            int quantity = 20)
        {
            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var medId = medicationId ?? (await CreateMedicationAsync(adminClient)).Id;

            var payload = new CreatePrescriptionDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Notes = "Test prescription",
                Items = new List<CreatePrescriptionItemDto>
                {
                    new()
                    {
                        MedicationId = medId,
                        Dosage = "1 capsule",
                        Frequency = "Twice daily",
                        DurationInDays = 10,
                        Quantity = quantity,
                        Instructions = "Take with food"
                    }
                }
            };

            var response = await doctorClient.PostAsJsonAsync(BaseUrl, payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PrescriptionDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }
    }
}
