using FluentValidation;
using Hospital.Application.DTOs.Pharmacy;

namespace Hospital.Application.Validations
{
    public class CreatePrescriptionItemDtoValidator : AbstractValidator<CreatePrescriptionItemDto>
    {
        public CreatePrescriptionItemDtoValidator()
        {
            RuleFor(x => x.MedicationId)
                .NotEmpty().WithMessage("Medication ID is required.");

            RuleFor(x => x.Dosage)
                .NotEmpty().WithMessage("Dosage is required.")
                .MaximumLength(100).WithMessage("Dosage cannot exceed 100 characters.");

            RuleFor(x => x.Frequency)
                .NotEmpty().WithMessage("Frequency is required.")
                .MaximumLength(100).WithMessage("Frequency cannot exceed 100 characters.");

            RuleFor(x => x.DurationInDays)
                .GreaterThan(0).WithMessage("Duration must be at least 1 day.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

            RuleFor(x => x.Instructions)
                .MaximumLength(500).WithMessage("Instructions cannot exceed 500 characters.");
        }
    }
}
