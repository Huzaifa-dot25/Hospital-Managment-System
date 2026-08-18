using FluentValidation;
using Hospital.Application.DTOs.Appointment;
using System;

namespace Hospital.Application.Validations
{
    public class UpdateAppointmentDtoValidator : AbstractValidator<UpdateAppointmentDto>
    {
        public UpdateAppointmentDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Appointment ID is required.");

            RuleFor(x => x.AppointmentDate)
                .NotEmpty().WithMessage("Appointment Date is required.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required.")
                .MaximumLength(200).WithMessage("Reason cannot exceed 200 characters.");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid Appointment Status.");
        }
    }
}
