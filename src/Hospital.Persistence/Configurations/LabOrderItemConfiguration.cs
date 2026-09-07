using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class LabOrderItemConfiguration : IEntityTypeConfiguration<LabOrderItem>
    {
        public void Configure(EntityTypeBuilder<LabOrderItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ResultValue)
                .HasMaxLength(500);

            builder.Property(x => x.Unit)
                .HasMaxLength(50);

            builder.Property(x => x.ReferenceRange)
                .HasMaxLength(200);

            builder.Property(x => x.Remarks)
                .HasMaxLength(1000);

            builder.HasOne(x => x.LabTest)
                .WithMany(t => t.OrderItems)
                .HasForeignKey(x => x.LabTestId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
