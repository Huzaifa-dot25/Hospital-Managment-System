using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class LabTestConfiguration : IEntityTypeConfiguration<LabTest>
    {
        public void Configure(EntityTypeBuilder<LabTest> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.Property(x => x.Category)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.Price)
                .HasPrecision(18, 2);

            builder.Property(x => x.SampleType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.ReferenceRange)
                .HasMaxLength(200);

            builder.Property(x => x.Unit)
                .HasMaxLength(50);
        }
    }
}
