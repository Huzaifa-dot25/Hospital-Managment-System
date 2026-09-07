using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
    {
        public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Dosage)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Frequency)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Instructions)
                .HasMaxLength(500);

            builder.HasOne(x => x.Medication)
                .WithMany(m => m.PrescriptionItems)
                .HasForeignKey(x => x.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
