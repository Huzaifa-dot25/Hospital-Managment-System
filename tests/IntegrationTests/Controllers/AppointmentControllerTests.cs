using Hospital.Shared.Constants;
using Hospital.Shared.Queries;

namespace IntegrationTests.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // APPOINTMENTCONTROLLERTESTS
    //
    // Tests the full Appointment CRUD pipeline end-to-end including:
    //   - Role-based authorization:
    //       * Staff (Admin, Receptionist, Doctor, Nurse) can browse all appointments
    //       * Patient CANNOT browse all appointments (403 — critical HIPAA data privacy)
    //       * Patient and Receptionist CAN book appointments (201)
    //       * Nurse CANNOT book appointments (403)
    //       * Receptionist and Doctor CAN update appointments (200)
    //       * Patient CANNOT update appointments directly (403)
    //       * Only Admin/SuperAdmin CAN delete appointments (403 for others)
    //   - Cross-entity foreign key checks: Patient and Doctor must both exist (404)
    //   - Eager loading: PatientName and DoctorName populated on retrieval
    //   - Validation: AppointmentDate must be in the future (400 if past)
    //   - Soft-delete: deleted appointment returns 404 on subsequent get
    // ─────────────────────────────────────────────────────────────────────────
    public class AppointmentControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string BaseUrl = "/api/v1/appointment";

        public AppointmentControllerTests(CustomWebApplicationFactory factory)
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
                .ReadFromJsonAsync<ApiResponse<PagedResponse<AppointmentDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.PageNumber.Should().Be(1);
            body.Data.PageSize.Should().Be(10);
            body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAll_AsReceptionist_Returns200WithPagedResponse()
        {
            // ARRANGE
            var client = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            // ACT
            var response = await client.GetAsync(BaseUrl);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResponse<AppointmentDto>>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAll_AsPatientRole_Returns403()
        {
            // ARRANGE — Patients must not browse all hospital appointments (HIPAA rule)
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
        public async Task GetById_WhenAppointmentExists_Returns200WithPatientAndDoctorNames()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var created = await CreateAppointmentAsync(receptionistClient, patient.Id, doctor.Id);

            // ACT — Doctor reads the appointment
            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var response = await doctorClient.GetAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().Be(created.Id);
            body.Data.PatientId.Should().Be(patient.Id);
            body.Data.DoctorId.Should().Be(doctor.Id);

            // Verify eager-loading populated navigation names
            body.Data.PatientName.Should().NotBeNullOrWhiteSpace();
            body.Data.DoctorName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetById_WhenAppointmentDoesNotExist_Returns404()
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
        public async Task Create_AsReceptionist_Returns201WithCreatedAppointment()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var dto = BuildCreateAppointmentDto(patient.Id, doctor.Id);

            // ACT
            var response = await receptionistClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestJsonOptions.Default);

            body!.Success.Should().BeTrue();
            body.Data!.Id.Should().NotBeEmpty();
            body.Data.PatientId.Should().Be(patient.Id);
            body.Data.DoctorId.Should().Be(doctor.Id);
            body.Data.Reason.Should().Be(dto.Reason);
            body.Data.Status.Should().Be(AppointmentStatus.Scheduled);
            body.Data.PatientName.Should().NotBeNullOrWhiteSpace();
            body.Data.DoctorName.Should().NotBeNullOrWhiteSpace();

            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_AsPatient_Returns201()
        {
            // ARRANGE — Patients can book their own appointments
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var dto = BuildCreateAppointmentDto(patient.Id, doctor.Id);

            // ACT
            var response = await patientClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task Create_AsNurse_Returns403()
        {
            // ARRANGE — Nurse role cannot book appointments
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var nurseClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Nurse);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var dto = BuildCreateAppointmentDto(patient.Id, doctor.Id);

            // ACT
            var response = await nurseClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WithoutToken_Returns401()
        {
            // ARRANGE
            var client = _factory.CreateUnauthenticatedClient();
            var dto = BuildCreateAppointmentDto(Guid.NewGuid(), Guid.NewGuid());

            // ACT
            var response = await client.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_WhenPatientDoesNotExist_Returns404()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var doctor = await CreateDoctorAsync(adminClient);
            var nonExistentPatientId = Guid.NewGuid();

            var dto = BuildCreateAppointmentDto(nonExistentPatientId, doctor.Id);

            // ACT
            var response = await receptionistClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WhenDoctorDoesNotExist_Returns404()
        {
            // ARRANGE
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var patient = await CreatePatientAsync(receptionistClient);
            var nonExistentDoctorId = Guid.NewGuid();

            var dto = BuildCreateAppointmentDto(patient.Id, nonExistentDoctorId);

            // ACT
            var response = await receptionistClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WithPastAppointmentDate_Returns400()
        {
            // ARRANGE — Appointment date must be in the future
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = BuildCreateAppointmentDto(patient.Id, doctor.Id);
            dto.AppointmentDate = DateTime.UtcNow.AddDays(-1); // in the past

            // ACT
            var response = await receptionistClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WithEmptyReason_Returns400()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);

            var dto = BuildCreateAppointmentDto(patient.Id, doctor.Id);
            dto.Reason = "";

            // ACT
            var response = await receptionistClient.PostAsJsonAsync(BaseUrl, dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_AsReceptionist_WhenAppointmentExists_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var created = await CreateAppointmentAsync(receptionistClient, patient.Id, doctor.Id);

            var newDate = DateTime.UtcNow.AddDays(14);
            var updateDto = new UpdateAppointmentDto
            {
                Id = created.Id,
                AppointmentDate = newDate,
                Reason = "Rescheduled follow-up",
                Status = AppointmentStatus.Cancelled,
                Notes = "Patient requested date change"
            };

            // ACT
            var response = await receptionistClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify persistence
            var getResponse = await receptionistClient.GetAsync($"{BaseUrl}/{created.Id}");
            var body = await getResponse.Content
                .ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestJsonOptions.Default);

            body!.Data!.Reason.Should().Be("Rescheduled follow-up");
            body.Data.Status.Should().Be(AppointmentStatus.Cancelled);
            body.Data.Notes.Should().Be("Patient requested date change");
        }

        [Fact]
        public async Task Update_AsDoctor_Returns200()
        {
            // ARRANGE — Doctor can update status & add clinical notes
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var created = await CreateAppointmentAsync(receptionistClient, patient.Id, doctor.Id);

            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);
            var updateDto = new UpdateAppointmentDto
            {
                Id = created.Id,
                AppointmentDate = created.AppointmentDate,
                Reason = created.Reason,
                Status = AppointmentStatus.Completed,
                Notes = "Routine exam completed, prescription given."
            };

            // ACT
            var response = await doctorClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Update_AsPatient_Returns403()
        {
            // ARRANGE — Patient cannot directly call PUT to modify status or clinical notes
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var created = await CreateAppointmentAsync(receptionistClient, patient.Id, doctor.Id);

            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);
            var updateDto = new UpdateAppointmentDto
            {
                Id = created.Id,
                AppointmentDate = created.AppointmentDate.AddDays(1),
                Reason = "Patient self-reschedule attempt",
                Status = AppointmentStatus.Scheduled,
                Notes = ""
            };

            // ACT
            var response = await patientClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Update_WhenIdMismatch_Returns400()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var created = await CreateAppointmentAsync(receptionistClient, patient.Id, doctor.Id);

            var updateDto = new UpdateAppointmentDto
            {
                Id = Guid.NewGuid(), // different from URL id
                AppointmentDate = DateTime.UtcNow.AddDays(5),
                Reason = "Mismatch test",
                Status = AppointmentStatus.Scheduled,
                Notes = ""
            };

            // ACT
            var response = await receptionistClient.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_WhenAppointmentDoesNotExist_Returns404()
        {
            // ARRANGE
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);
            var nonExistentId = Guid.NewGuid();

            var updateDto = new UpdateAppointmentDto
            {
                Id = nonExistentId,
                AppointmentDate = DateTime.UtcNow.AddDays(5),
                Reason = "Not found test",
                Status = AppointmentStatus.Scheduled,
                Notes = ""
            };

            // ACT
            var response = await receptionistClient.PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", updateDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WithoutToken_Returns401()
        {
            // ARRANGE
            var client = _factory.CreateUnauthenticatedClient();
            var dummyId = Guid.NewGuid();

            // ACT
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{dummyId}",
                new UpdateAppointmentDto { Id = dummyId });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_AsAdmin_WhenAppointmentExists_Returns200()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var created = await CreateAppointmentAsync(receptionistClient, patient.Id, doctor.Id);

            // ACT
            var response = await adminClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Soft-delete verification: subsequent GET must return 404
            var getResponse = await adminClient.GetAsync($"{BaseUrl}/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
                because: "soft-deleted appointment must be excluded by global query filter");
        }

        [Fact]
        public async Task Delete_AsReceptionist_Returns403()
        {
            // ARRANGE — Only Admin and above can hard/soft delete appointment records
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var created = await CreateAppointmentAsync(receptionistClient, patient.Id, doctor.Id);

            // ACT
            var response = await receptionistClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_AsDoctor_Returns403()
        {
            // ARRANGE
            var adminClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Admin);
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var created = await CreateAppointmentAsync(receptionistClient, patient.Id, doctor.Id);

            var doctorClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Doctor);

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
            var receptionistClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Receptionist);

            var patient = await CreatePatientAsync(receptionistClient);
            var doctor = await CreateDoctorAsync(adminClient);
            var created = await CreateAppointmentAsync(receptionistClient, patient.Id, doctor.Id);

            var patientClient = await AuthHelper.GetAuthenticatedClientAsync(_factory, Roles.Patient);

            // ACT
            var response = await patientClient.DeleteAsync($"{BaseUrl}/{created.Id}");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_WhenAppointmentDoesNotExist_Returns404()
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

        private static CreateAppointmentDto BuildCreateAppointmentDto(Guid patientId, Guid doctorId) =>
            new()
            {
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentDate = DateTime.UtcNow.AddDays(7),
                Reason = "Regular checkup"
            };

        private static async Task<PatientDto> CreatePatientAsync(HttpClient client)
        {
            var payload = new CreatePatientDto
            {
                FirstName = $"Pat_{Guid.NewGuid():N}"[..8],
                LastName = "Patient",
                DateOfBirth = DateTime.UtcNow.AddYears(-28),
                Gender = Gender.Male,
                BloodGroup = BloodGroup.APositive,
                ContactNumber = "+1234567890",
                Address = "456 Main Blvd",
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
                Description = "Department for appointment test"
            };
            var deptResponse = await adminClient.PostAsJsonAsync("/api/v1/department", deptPayload);
            deptResponse.EnsureSuccessStatusCode();
            var deptBody = await deptResponse.Content
                .ReadFromJsonAsync<ApiResponse<DepartmentDto>>(TestJsonOptions.Default);

            var docPayload = new CreateDoctorDto
            {
                FirstName = $"Doc_{Guid.NewGuid():N}"[..8],
                LastName = "Watson",
                Specialization = "Cardiology",
                LicenseNumber = $"LIC-{Guid.NewGuid():N}"[..12],
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

        private static async Task<AppointmentDto> CreateAppointmentAsync(
            HttpClient client, Guid patientId, Guid doctorId)
        {
            var payload = BuildCreateAppointmentDto(patientId, doctorId);
            var response = await client.PostAsJsonAsync(BaseUrl, payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<AppointmentDto>>(TestJsonOptions.Default);

            return body!.Data!;
        }
    }
}
