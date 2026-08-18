using System;

namespace Hospital.Application.DTOs.Doctor
{
    public class CreateDoctorDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
    }
}
