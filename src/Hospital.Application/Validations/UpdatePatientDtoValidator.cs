using FluentValidation;
using Hospital.Application.DTOs.Patient;
using System;

namespace Hospital.Application.Validations
{
    public class UpdatePatientDtoValidator : AbstractValidator<UpdatePatientDto>
    {
        public UpdatePatientDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Patient ID is required.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First Name is required.")
                .MaximumLength(50).WithMessage("First Name cannot exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last Name is required.")
                .MaximumLength(50).WithMessage("Last Name cannot exceed 50 characters.");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of Birth is required.")
                .LessThan(DateTime.UtcNow).WithMessage("Date of Birth cannot be in the future.");

            RuleFor(x => x.ContactNumber)
                .NotEmpty().WithMessage("Contact Number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid Contact Number format.");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required.")
                .MaximumLength(200).WithMessage("Address cannot exceed 200 characters.");

            RuleFor(x => x.EmergencyContactName)
                .NotEmpty().WithMessage("Emergency Contact Name is required.")
                .MaximumLength(100).WithMessage("Emergency Contact Name cannot exceed 100 characters.");

            RuleFor(x => x.EmergencyContactNumber)
                .NotEmpty().WithMessage("Emergency Contact Number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid Emergency Contact Number format.");
        }
    }
}
