using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // MEDICALRECORDCONTROLLERTESTS
    //
    // Tests the full Medical Record CRUD pipeline end-to-end including:
    //   - Clinical access controls (Doctor/Nurse can view, Doctor/Admin can create & update)
    //   - Non-clinical roles (Receptionist, Patient) blocked from browsing all records (403)
    //   - Patients can view individual medical records by ID
    //   - Eager-loading: PatientName and DoctorName are populated
    //   - Foreign key validation: Patient and Doctor must exist (404)
    //   - Validation: empty diagnosis/symptoms/treatment returns 400
    //   - Soft-delete: restricted to Admin, deleted records excluded from subsequent gets
    //   - Unauthenticated requests return 401
    // ─────────────────────────────────────────────────────────────────────────
    public class MedicalRecordControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/medicalrecord";

        public MedicalRecordControllerTests(CustomWebApplicationFactory factory)
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
                .ReadFromJsonAsync<ApiResponse<PagedResponse<MedicalRecordDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.PageNumber.Should().Be(1);
            body.Data.PageSize.Should().Be(10);
            body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAll_AsNurse_Returns200WithPagedResponse()
        {
            // ARRANGE — Clinical staff (Nurse) can view records
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Nurse);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResponse<MedicalRecordDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAll_AsPatient_Returns403()
        {
            // ARRANGE — Patients must not browse all hospital medical records
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetAll_AsReceptionist_Returns403()
        {
            // ARRANGE — Non-clinical front desk staff cannot browse medical records
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

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
        public async Task GetById_WhenRecordExists_Returns200WithDetails()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var created = await CreateMedicalRecordAsync(adminClient);

            // ACT — Read by doctor
            var response = await doctorClient.GetAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<MedicalRecordDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().Be(created.Id);
            body.Data.Diagnosis.Should().Be(created.Diagnosis);
            body.Data.PatientName.Should().NotBeNullOrWhiteSpace();
            body.Data.DoctorName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetById_AsPatient_Returns200()
        {
            // ARRANGE — Patients are permitted to read their medical record by ID
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateMedicalRecordAsync(adminClient);

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
        public async Task Create_AsDoctor_Returns201WithCreatedRecord()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = new CreateMedicalRecordDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = "Type 2 Diabetes Mellitus",
                Symptoms = "Fatigue, polydipsia, polyuria",
                Treatment = "Metformin 500mg daily, lifestyle modifications",
                Prescription = "Metformin 500mg #60",
                Notes = "A1C target < 7.0%"
            };

            // ACT
            var response = await doctorClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<MedicalRecordDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().NotBeEmpty();
            body.Data.Diagnosis.Should().Be(dto.Diagnosis);
            body.Data.PatientName.Should().NotBeNullOrWhiteSpace();
            body.Data.DoctorName.Should().NotBeNullOrWhiteSpace();

            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_AsAdmin_Returns201()
        {
            // ARRANGE — Admin can also create records
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = new CreateMedicalRecordDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = "Hypertension",
                Symptoms = "Occasional headache",
                Treatment = "Amlodipine 5mg",
                Prescription = "Amlodipine 5mg #30"
            };

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task Create_AsNurse_Returns403()
        {
            // ARRANGE — Nurses cannot create primary medical records
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var nurseClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Nurse);

            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = new CreateMedicalRecordDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = "Common Cold",
                Symptoms = "Rhinorrhea",
                Treatment = "Rest"
            };

            // ACT
            var response = await nurseClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_AsPatient_Returns403()
        {
            // ARRANGE — Patients cannot self-create medical records
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = new CreateMedicalRecordDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = "Self Diagnosis",
                Symptoms = "Cough",
                Treatment = "Tea"
            };

            // ACT
            var response = await patientClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WhenPatientDoesNotExist_Returns404()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = new CreateMedicalRecordDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = doctor.Id,
                Diagnosis = "Test",
                Symptoms = "Test",
                Treatment = "Test"
            };

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WhenDoctorDoesNotExist_Returns404()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var patient = await CreatePatientAsync(adminClient);

            var dto = new CreateMedicalRecordDto
            {
                PatientId = patient.Id,
                DoctorId = Guid.NewGuid(),
                Diagnosis = "Test",
                Symptoms = "Test",
                Treatment = "Test"
            };

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WithEmptyDiagnosis_Returns400()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = new CreateMedicalRecordDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = "", // required
                Symptoms = "Cough",
                Treatment = "Rest"
            };

            // ACT
            var response = await adminClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WithoutToken_Returns401()
        {
            // ARRANGE
            var client = _factory.CreateUnauthenticatedClient();
            var dto = new CreateMedicalRecordDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "Test",
                Symptoms = "Test",
                Treatment = "Test"
            };

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_AsDoctor_WhenRecordExists_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var created = await CreateMedicalRecordAsync(adminClient);

            var updateDto = new UpdateMedicalRecordDto
            {
                Id = created.Id,
                Diagnosis = "Hypertension Stage 2 (Updated)",
                Symptoms = "Headache resolved",
                Treatment = "Increased Amlodipine to 10mg",
                Prescription = "Amlodipine 10mg #30",
                Notes = "Patient responsive to treatment"
            };

            // ACT
            var response = await doctorClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify persistence
            var getResponse = await doctorClient.GetAsync($"{BaseUrl}/{created.Id}");
            var body = await getResponse.Content
                .ReadFromJsonAsync<ApiResponse<MedicalRecordDto>>(TestJsonOptions.Default);

            body!.Data!.Diagnosis.Should().Be("Hypertension Stage 2 (Updated)");
            body.Data.Treatment.Should().Be("Increased Amlodipine to 10mg");
        }

        [Fact]
        public async Task Update_WhenIdMismatch_Returns400()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateMedicalRecordAsync(adminClient);

            var updateDto = new UpdateMedicalRecordDto
            {
                Id = Guid.NewGuid(), // different from URL
                Diagnosis = "Mismatch",
                Symptoms = "Test",
                Treatment = "Test"
            };

            // ACT
            var response = await adminClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_WhenRecordDoesNotExist_Returns404()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var nonExistentId = Guid.NewGuid();

            var updateDto = new UpdateMedicalRecordDto
            {
                Id = nonExistentId,
                Diagnosis = "Not Found",
                Symptoms = "Test",
                Treatment = "Test"
            };

            // ACT
            var response = await adminClient.PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_AsPatient_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var created = await CreateMedicalRecordAsync(adminClient);

            var updateDto = new UpdateMedicalRecordDto
            {
                Id = created.Id,
                Diagnosis = "Patient Tamper Attempt",
                Symptoms = "None",
                Treatment = "None"
            };

            // ACT
            var response = await patientClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

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
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{dummyId}",
                new UpdateMedicalRecordDto { Id = dummyId });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_AsAdmin_WhenRecordExists_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var created = await CreateMedicalRecordAsync(adminClient);

            // ACT
            var response = await adminClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Soft-deleted record must return 404 on subsequent get
            var getResponse = await adminClient.GetAsync($"{BaseUrl}/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
                because: "soft-deleted medical records must be excluded by global query filter");
        }

        [Fact]
        public async Task Delete_AsDoctor_Returns403()
        {
            // ARRANGE — Doctors cannot delete medical records (Admin only)
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

            var created = await CreateMedicalRecordAsync(adminClient);

            // ACT
            var response = await doctorClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_AsPatient_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var created = await CreateMedicalRecordAsync(adminClient);

            // ACT
            var response = await patientClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_WhenRecordDoesNotExist_Returns404()
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

        private static async Task<PatientDto> CreatePatientAsync(HttpClient client)
        {
            var payload = new CreatePatientDto
            {
                FirstName = $"Pat_{Guid.NewGuid():N}"[..8],
                LastName = "MedicalRecordTest",
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                Gender = Gender.Female,
                BloodGroup = BloodGroup.OPositive,
                ContactNumber = "+1234567890",
                Address = "Medical Record Test Address",
                EmergencyContactName = "Emergency Contact",
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
                Description = "Department for medical record test"
            };
            var deptResponse = await adminClient.PostAsJsonAsync("/api/v1/department", deptPayload);
            deptResponse.EnsureSuccessStatusCode();
            var deptBody = await deptResponse.Content
                .ReadFromJsonAsync<ApiResponse<DepartmentDto>>(TestJsonOptions.Default);

            var docPayload = new CreateDoctorDto
            {
                FirstName = $"Doc_{Guid.NewGuid():N}"[..8],
                LastName = "House",
                Specialization = "Internal Medicine",
                LicenseNumber = $"LIC-{Guid.NewGuid():N}"[..12],
                YearsOfExperience = 15,
                ContactNumber = "+1234567890",
                DepartmentId = deptBody!.Data!.Id
            };

            var docResponse = await adminClient.PostAsJsonAsync("/api/v1/doctor", docPayload);
            docResponse.EnsureSuccessStatusCode();
            var docBody = await docResponse.Content
                .ReadFromJsonAsync<ApiResponse<DoctorDto>>(TestJsonOptions.Default);

            return docBody!.Data!;
        }

        private static async Task<MedicalRecordDto> CreateMedicalRecordAsync(HttpClient adminClient)
        {
            var patient = await CreatePatientAsync(adminClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var payload = new CreateMedicalRecordDto
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = "Acute Bronchitis",
                Symptoms = "Cough and low grade fever",
                Treatment = "Rest, increased fluid intake, acetaminophen",
                Prescription = "Acetaminophen 500mg prn",
                Notes = "Re-evaluate if fever persists"
            };

            var response = await adminClient.PostAsJsonAsync(BaseUrl, payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<MedicalRecordDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }
    }
}
