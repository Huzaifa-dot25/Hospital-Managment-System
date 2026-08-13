using Hospital.Domain.Common;

namespace Hospital.Domain.Entities
{
    public class Doctor : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string ContactNumber { get; set; } = string.Empty;

        // Foreign Key
        public Guid DepartmentId { get; set; }
        // Navigation Property
        public Department Department { get; set; } = null!;
    }
}
