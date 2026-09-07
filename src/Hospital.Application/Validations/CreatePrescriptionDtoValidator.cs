using FluentValidation;
using Hospital.Application.DTOs.Pharmacy;

namespace Hospital.Application.Validations
{
    public class CreatePrescriptionDtoValidator : AbstractValidator<CreatePrescriptionDto>
    {
        public CreatePrescriptionDtoValidator()
        {
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("Patient ID is required.");

            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("Doctor ID is required.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("A prescription must contain at least one item.");

            RuleForEach(x => x.Items).SetValidator(new CreatePrescriptionItemDtoValidator());

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
        }
    }
}
