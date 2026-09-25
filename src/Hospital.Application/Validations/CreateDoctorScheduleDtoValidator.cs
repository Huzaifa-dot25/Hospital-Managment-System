using FluentValidation;
using Hospital.Application.DTOs.Doctor;
using System;

namespace Hospital.Application.Validations
{
    public class CreateDoctorScheduleDtoValidator : AbstractValidator<CreateDoctorScheduleDto>
    {
        public CreateDoctorScheduleDtoValidator()
        {
            RuleFor(x => x.DayOfWeek)
                .IsInEnum().WithMessage("Invalid day of week.");

            RuleFor(x => x.StartTime)
                .LessThan(x => x.EndTime).WithMessage("Start time must be before end time.");

            RuleFor(x => x.MaxPatients)
                .GreaterThan(0).WithMessage("Max patients must be greater than 0.");
        }
    }
}
