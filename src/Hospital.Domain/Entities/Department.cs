using System.Collections.Generic;
using Hospital.Domain.Common;

namespace Hospital.Domain.Entities
{
    public class Department : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Navigation Property: A department has many doctors
        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}
