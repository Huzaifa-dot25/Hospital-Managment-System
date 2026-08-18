using FluentValidation;
using Hospital.Application.DTOs.Appointment;
using System;

namespace Hospital.Application.Validations
{
    public class CreateAppointmentDtoValidator : AbstractValidator<CreateAppointmentDto>
    {
        public CreateAppointmentDtoValidator()
        {
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("Patient ID is required.");

            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("Doctor ID is required.");

            RuleFor(x => x.AppointmentDate)
                .NotEmpty().WithMessage("Appointment Date is required.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Appointment Date must be in the future.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required.")
                .MaximumLength(200).WithMessage("Reason cannot exceed 200 characters.");
        }
    }
}
