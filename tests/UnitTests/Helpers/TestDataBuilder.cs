using Hospital.Application.DTOs.Appointment;
using Hospital.Application.DTOs.Department;
using Hospital.Application.DTOs.Doctor;
using Hospital.Application.DTOs.Patient;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace UnitTests.Helpers
{
    // ─────────────────────────────────────────────────────────────────────────
    // TESTDATABUILDER — The Test Object Factory
    //
    // PURPOSE:
    //   Centralise creation of all test data objects in one place.
    //   Every test that needs a Patient, Doctor, Department, or Appointment
    //   calls one of these static methods instead of creating objects inline.
    //
    // WHY THIS MATTERS:
    //   Imagine the Patient entity gets a new required property (e.g. NationalId).
    //   Without this class, you'd need to fix 30 different test files.
    //   With this class, you fix ONE method and all tests are healed instantly.
    //
    // DESIGN DECISIONS:
    //   - All methods are STATIC — no need to instantiate the builder itself.
    //   - Methods accept OPTIONAL parameters for fields tests care about.
    //   - All other fields get sensible defaults so tests stay focused.
    //   - Dates use DateTime.UtcNow.AddDays(N) so they're always valid relative
    //     to "now" regardless of when the test runs.
    //
    // PATTERN: Test Object Mother (a variation of Builder pattern for tests).
    // ─────────────────────────────────────────────────────────────────────────
    public static class TestDataBuilder
    {
        // ─────────────────────────────────────────────────────────────────────
        // DEPARTMENT BUILDERS
        // Departments are the simplest entity — just Name + Description.
        // They are the root of the domain (Doctors belong to Departments).
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a Department entity with sensible defaults.
        /// Pass a custom name only when the test cares about a specific name.
        /// </summary>
        public static Department CreateDepartment(
            Guid? id = null,
            string name = "Cardiology",
            string description = "Heart and cardiovascular care")
        {
            return new Department
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Description = description,
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a valid CreateDepartmentDto — used in Create tests.
        /// The DTO is what the API client sends. It has no Id (the DB generates it).
        /// </summary>
        public static CreateDepartmentDto CreateDepartmentDto(
            string name = "Cardiology",
            string description = "Heart and cardiovascular care")
        {
            return new CreateDepartmentDto
            {
                Name = name,
                Description = description
            };
        }

        /// <summary>
        /// Creates a valid UpdateDepartmentDto — used in Update tests.
        /// Unlike CreateDepartmentDto, the Update DTO carries the existing Id.
        /// </summary>
        public static UpdateDepartmentDto UpdateDepartmentDto(
            Guid? id = null,
            string name = "Neurology",
            string description = "Brain and nervous system care")
        {
            return new UpdateDepartmentDto
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Description = description
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // DOCTOR BUILDERS
        // Doctors have a foreign key to Department (DepartmentId).
        // When creating a Doctor, we always provide a DepartmentId so the
        // business rule check in DoctorService can be mocked correctly.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a Doctor entity with a linked Department pre-populated.
        /// The Department navigation property is set so AutoMapper can read
        /// doctor.Department.Name without a NullReferenceException.
        /// </summary>
        public static Doctor CreateDoctor(
            Guid? id = null,
            Guid? departmentId = null,
            string firstName = "Ahmed",
            string lastName = "Hassan",
            string specialization = "Cardiology",
            string licenseNumber = "LIC-001")
        {
            // The actual departmentId used in the entity
            var deptId = departmentId ?? Guid.NewGuid();

            return new Doctor
            {
                Id = id ?? Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Specialization = specialization,
                LicenseNumber = licenseNumber,
                YearsOfExperience = 10,
                ContactNumber = "+1234567890",
                DepartmentId = deptId,

                // Pre-populate the navigation property so tests that use
                // Doctor.Department.Name don't crash with NullReferenceException.
                // In production, EF Core populates this via eager loading (Include).
                // In tests, we simulate that manually.
                Department = new Department
                {
                    Id = deptId,
                    Name = "Cardiology",
                    Description = "Heart and cardiovascular care"
                },
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a valid CreateDoctorDto — the incoming API request body for creating a doctor.
        /// </summary>
        public static CreateDoctorDto CreateDoctorDto(
            Guid? departmentId = null,
            string firstName = "Ahmed",
            string lastName = "Hassan")
        {
            return new CreateDoctorDto
            {
                FirstName = firstName,
                LastName = lastName,
                Specialization = "Cardiology",
                LicenseNumber = "LIC-001",
                YearsOfExperience = 10,
                ContactNumber = "+1234567890",
                DepartmentId = departmentId ?? Guid.NewGuid()
            };
        }

        /// <summary>
        /// Creates a valid UpdateDoctorDto — the incoming API request body for updating a doctor.
        /// </summary>
        public static UpdateDoctorDto UpdateDoctorDto(
            Guid? id = null,
            Guid? departmentId = null)
        {
            return new UpdateDoctorDto
            {
                Id = id ?? Guid.NewGuid(),
                FirstName = "Khaled",
                LastName = "Ali",
                Specialization = "Neurology",
                LicenseNumber = "LIC-002",
                YearsOfExperience = 15,
                ContactNumber = "+9876543210",
                DepartmentId = departmentId ?? Guid.NewGuid()
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // PATIENT BUILDERS
        // Patients are standalone — they don't have a foreign key to other
        // entities at the base level (Appointments link them to Doctors).
        // Date-of-birth is set to 30 years ago so PatientDto.Age computes correctly.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a Patient entity with complete demographic data.
        /// DateOfBirth is always 30 years ago so Age = 30 consistently.
        /// </summary>
        public static Patient CreatePatient(
            Guid? id = null,
            string firstName = "Sara",
            string lastName = "Ahmed",
            Gender gender = Gender.Female)
        {
            return new Patient
            {
                Id = id ?? Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                Gender = gender,
                BloodGroup = BloodGroup.APositive,
                ContactNumber = "+1234567890",
                Address = "123 Nile Street, Cairo",
                EmergencyContactName = "Mohamed Ahmed",
                EmergencyContactNumber = "+9876543210",
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a valid CreatePatientDto — the incoming API request body for registering a patient.
        /// Contact number format +1234567890 matches the regex in CreatePatientDtoValidator.
        /// DateOfBirth is 25 years ago — safely in the past, passes the "LessThan(now)" rule.
        /// </summary>
        public static CreatePatientDto CreatePatientDto(
            string firstName = "Sara",
            string lastName = "Ahmed")
        {
            return new CreatePatientDto
            {
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                Gender = Gender.Female,
                BloodGroup = BloodGroup.APositive,
                ContactNumber = "+1234567890",
                Address = "123 Nile Street, Cairo",
                EmergencyContactName = "Mohamed Ahmed",
                EmergencyContactNumber = "+9876543210"
            };
        }

        /// <summary>
        /// Creates a valid UpdatePatientDto — the incoming API request body for updating a patient.
        /// </summary>
        public static UpdatePatientDto UpdatePatientDto(Guid? id = null)
        {
            return new UpdatePatientDto
            {
                Id = id ?? Guid.NewGuid(),
                FirstName = "Sara",
                LastName = "Hassan",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                Gender = Gender.Female,
                BloodGroup = BloodGroup.BPositive,
                ContactNumber = "+1122334455",
                Address = "456 Pyramids Road, Giza",
                EmergencyContactName = "Ali Hassan",
                EmergencyContactNumber = "+5566778899"
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // APPOINTMENT BUILDERS
        // Appointments are the most complex entity: they link Patient + Doctor.
        // AppointmentDate is always in the FUTURE (+1 day minimum) so the
        // "GreaterThan(DateTime.UtcNow)" validation rule always passes.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an Appointment entity with Patient and Doctor navigation
        /// properties pre-populated (simulating EF eager loading).
        /// </summary>
        public static Appointment CreateAppointment(
            Guid? id = null,
            Guid? patientId = null,
            Guid? doctorId = null,
            AppointmentStatus status = AppointmentStatus.Scheduled)
        {
            var pId = patientId ?? Guid.NewGuid();
            var dId = doctorId ?? Guid.NewGuid();

            return new Appointment
            {
                Id = id ?? Guid.NewGuid(),
                PatientId = pId,
                DoctorId = dId,
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                Reason = "Regular checkup",
                Status = status,
                Notes = string.Empty,

                // Pre-populate navigation properties so AutoMapper can read
                // appointment.Patient.FirstName and appointment.Doctor.FirstName
                Patient = new Patient
                {
                    Id = pId,
                    FirstName = "Sara",
                    LastName = "Ahmed"
                },
                Doctor = new Doctor
                {
                    Id = dId,
                    FirstName = "Ahmed",
                    LastName = "Hassan"
                },
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a valid CreateAppointmentDto — the incoming API request body.
        /// AppointmentDate is 2 days in the future to safely pass the GreaterThan(UtcNow) rule.
        /// </summary>
        public static CreateAppointmentDto CreateAppointmentDto(
            Guid? patientId = null,
            Guid? doctorId = null)
        {
            return new CreateAppointmentDto
            {
                PatientId = patientId ?? Guid.NewGuid(),
                DoctorId = doctorId ?? Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddDays(2),
                Reason = "Regular checkup"
            };
        }

        /// <summary>
        /// Creates a valid UpdateAppointmentDto.
        /// </summary>
        public static UpdateAppointmentDto UpdateAppointmentDto(Guid? id = null)
        {
            return new UpdateAppointmentDto
            {
                Id = id ?? Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddDays(3),
                Reason = "Follow-up visit",
                Status = AppointmentStatus.Scheduled,
                Notes = "Patient requested earlier slot"
            };
        }
    }
}
