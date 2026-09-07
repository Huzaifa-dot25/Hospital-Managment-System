using Hospital.Shared.Models;
using System;

namespace Hospital.Shared.Queries
{
    public class LabOrderQueryParams : PaginationParams
    {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public int? Status { get; set; }
        public int? Priority { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
