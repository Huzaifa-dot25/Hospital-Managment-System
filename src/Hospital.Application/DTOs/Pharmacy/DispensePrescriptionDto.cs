using System;

namespace Hospital.Application.DTOs.Pharmacy
{
    public class DispensePrescriptionDto
    {
        public Guid PharmacistId { get; set; }
        public string? DispensingNotes { get; set; }
    }
}
