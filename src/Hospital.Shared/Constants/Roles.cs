namespace Hospital.Shared.Constants
{
    /// <summary>
    /// Centralised role name constants used across the entire system.
    ///
    /// Why constants instead of raw strings?
    ///
    /// Problem with raw strings:
    ///   [Authorize(Roles = "Admin")]   // works
    ///   [Authorize(Roles = "admin")]   // silently fails — case mismatch
    ///   [Authorize(Roles = "Adminn")]  // typo — 403 for everyone, no error
    ///
    /// With constants:
    ///   [Authorize(Roles = Roles.Admin)]  // compiler catches typos
    ///   // Change "Admin" → "HospitalAdmin" in ONE place, everywhere updates
    ///
    /// These match exactly what gets stored in the AspNetRoles table and
    /// embedded in JWT tokens as ClaimTypes.Role claims.
    /// </summary>
    public static class Roles
    {
        public const string SuperAdmin    = "SuperAdmin";
        public const string Admin         = "Admin";
        public const string Doctor        = "Doctor";
        public const string Receptionist  = "Receptionist";
        public const string Nurse         = "Nurse";
        public const string Pharmacist    = "Pharmacist";
        public const string LabTechnician = "LabTechnician";
        public const string Radiologist   = "Radiologist";
        public const string Cashier       = "Cashier";
        public const string Patient       = "Patient";
        public const string Accountant    = "Accountant";

        // ─── Composite role strings for [Authorize(Roles = "...")] ──────
        // ASP.NET Core [Authorize(Roles = "...")] accepts comma-separated role names.
        // These pre-built combinations are used frequently enough to be worth naming.

        /// <summary>All administrative staff who can manage system settings.</summary>
        public const string AdminAndAbove = $"{SuperAdmin},{Admin}";

        /// <summary>Staff who work at the front desk and handle patient registration.</summary>
        public const string FrontDesk = $"{SuperAdmin},{Admin},{Receptionist}";

        /// <summary>All clinical staff who interact with patients directly.</summary>
        public const string ClinicalStaff = $"{SuperAdmin},{Admin},{Doctor},{Nurse}";

        /// <summary>Any staff member (all roles except Patient).</summary>
        public const string AnyStaff =
            $"{SuperAdmin},{Admin},{Doctor},{Receptionist},{Nurse}," +
            $"{Pharmacist},{LabTechnician},{Radiologist},{Cashier},{Accountant}";
    }
}
