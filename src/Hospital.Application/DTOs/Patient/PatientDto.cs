using System;
using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs.Patient
{
    /// <summary>
    /// The Patient read DTO — what the API returns to clients.
    ///
    /// DTOs (Data Transfer Objects) are NOT entities.
    /// They are shaped specifically for the consumer (API client).
    ///
    /// Differences from the Patient entity:
    ///   + Age is computed (not stored in the database)
    ///   + Gender and BloodGroup return enum names (e.g. "Male") not integers
    ///   - No IsDeleted, CreatedBy etc. — clients don't need audit fields
    /// </summary>
    public class PatientDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>Full name — convenient for display, saves frontend string concat.</summary>
        public string FullName => $"{FirstName} {LastName}";

        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Calculated age in years.
        /// Stored in DB as DateOfBirth (never changes, always accurate).
        /// Age is computed here so the API always returns the current age.
        /// Storing age in the DB would require daily updates — unnecessary complexity.
        /// </summary>
        public int Age
        {
            get
            {
                var today = DateTime.UtcNow;
                var age = today.Year - DateOfBirth.Year;
                // If birthday hasn't occurred yet this year, subtract 1
                if (DateOfBirth.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public Gender Gender { get; set; }
        public BloodGroup BloodGroup { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactNumber { get; set; } = string.Empty;

        /// <summary>When the patient record was created (UTC).</summary>
        public DateTime CreatedDate { get; set; }
    }
}
