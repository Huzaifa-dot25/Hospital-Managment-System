using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.ToTable("Patients");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.LastName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.ContactNumber)
                .HasMaxLength(20);

            builder.Property(p => p.Address)
                .HasMaxLength(500);

            builder.Property(p => p.EmergencyContactName)
                .HasMaxLength(100);

            builder.Property(p => p.EmergencyContactNumber)
                .HasMaxLength(20);
        }
    }
}
