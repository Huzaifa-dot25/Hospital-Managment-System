using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.ToTable("Doctors");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.LastName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.Specialization)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.LicenseNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.ContactNumber)
                .HasMaxLength(20);

            builder.Property(d => d.DepartmentId)
                .IsRequired();
        }
    }
}
