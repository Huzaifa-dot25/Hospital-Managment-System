using System;

namespace Hospital.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Audit Properties
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        public DateTime? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
}
