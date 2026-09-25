using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Persistence.Configurations
{
    public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
    {
        public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DayOfWeek)
                   .IsRequired();

            builder.Property(x => x.StartTime)
                   .IsRequired();

            builder.Property(x => x.EndTime)
                   .IsRequired();

            builder.Property(x => x.MaxPatients)
                   .IsRequired();

            builder.HasOne(x => x.Doctor)
                   .WithMany(d => d.Schedules)
                   .HasForeignKey(x => x.DoctorId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
