using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount)
                .HasPrecision(18, 2);

            builder.Property(x => x.TransactionReference)
                .HasMaxLength(100);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);
        }
    }
}
