using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
    {
        public void Configure(EntityTypeBuilder<Medication> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.GenericName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Category)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.DosageForm)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Strength)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Price)
                .HasPrecision(18, 2);

            builder.Property(x => x.Manufacturer)
                .HasMaxLength(150);
        }
    }
}
