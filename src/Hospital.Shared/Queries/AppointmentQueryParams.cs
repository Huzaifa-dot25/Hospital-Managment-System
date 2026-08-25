using System;
using Hospital.Shared.Models;

namespace Hospital.Shared.Queries
{
    /// <summary>
    /// Query parameters for GET /api/v1/appointment.
    ///
    /// Status integer values:
    ///   0 = Scheduled, 1 = Completed, 2 = Cancelled, 3 = NoShow
    ///
    /// Example URLs:
    ///   GET /api/v1/appointment?status=0
    ///   GET /api/v1/appointment?doctorId=abc&amp;fromDate=2026-01-01&amp;toDate=2026-01-31
    ///   GET /api/v1/appointment?patientId=xyz&amp;sortBy=appointmentDate&amp;isDescending=true
    /// </summary>
    public class AppointmentQueryParams : PaginationParams
    {
        /// <summary>Filter by patient — returns all appointments for one patient.</summary>
        public Guid? PatientId { get; set; }

        /// <summary>Filter by doctor — returns a doctor's schedule.</summary>
        public Guid? DoctorId { get; set; }

        /// <summary>
        /// Filter by appointment status as integer.
        /// Using int instead of the enum so Shared doesn't reference Domain enums.
        /// The repository casts it to AppointmentStatus before querying.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>Return appointments on or after this date.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Return appointments on or before this date.</summary>
        public DateTime? ToDate { get; set; }
    }
}
