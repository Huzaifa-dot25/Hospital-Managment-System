using Hospital.Domain.Common;
using System;

namespace Hospital.Domain.Entities.Identity
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Revoked { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? ReasonRevoked { get; set; }
        public bool IsActive => Revoked == null && !IsExpired;

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}
