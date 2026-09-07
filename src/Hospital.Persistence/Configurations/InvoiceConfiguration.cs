using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(x => x.InvoiceNumber)
                .IsUnique();

            builder.Property(x => x.SubTotal)
                .HasPrecision(18, 2);

            builder.Property(x => x.TaxPercentage)
                .HasPrecision(5, 2);

            builder.Property(x => x.TaxAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.PaidAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            // Relationships
            builder.HasOne(x => x.Patient)
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Prescription)
                .WithMany()
                .HasForeignKey(x => x.PrescriptionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.LabOrder)
                .WithMany()
                .HasForeignKey(x => x.LabOrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.Invoice)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Payments)
                .WithOne(x => x.Invoice)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
