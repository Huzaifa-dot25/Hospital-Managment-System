using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class LabOrderConfiguration : IEntityTypeConfiguration<LabOrder>
    {
        public void Configure(EntityTypeBuilder<LabOrder> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ClinicalNotes)
                .HasMaxLength(1000);

            builder.Property(x => x.SampleCollectedBy)
                .HasMaxLength(150);

            // Relationships
            builder.HasOne(x => x.Patient)
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MedicalRecord)
                .WithMany()
                .HasForeignKey(x => x.MedicalRecordId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.LabOrder)
                .HasForeignKey(x => x.LabOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
