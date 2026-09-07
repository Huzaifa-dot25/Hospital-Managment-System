using Hospital.Domain.Common;
using System;
using System.Collections.Generic;

namespace Hospital.Domain.Entities
{
    public class Medication : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DosageForm { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Manufacturer { get; set; } = string.Empty;
        public bool RequiresPrescription { get; set; } = true;

        public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    }
}
