using FluentValidation;
using Hospital.Application.DTOs.Pharmacy;

namespace Hospital.Application.Validations
{
    public class UpdateMedicationDtoValidator : AbstractValidator<UpdateMedicationDto>
    {
        public UpdateMedicationDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Medication ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Medication Name is required.")
                .MaximumLength(150).WithMessage("Medication Name cannot exceed 150 characters.");

            RuleFor(x => x.GenericName)
                .NotEmpty().WithMessage("Generic Name is required.")
                .MaximumLength(150).WithMessage("Generic Name cannot exceed 150 characters.");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required.")
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters.");

            RuleFor(x => x.DosageForm)
                .NotEmpty().WithMessage("Dosage Form is required.")
                .MaximumLength(50).WithMessage("Dosage Form cannot exceed 50 characters.");

            RuleFor(x => x.Strength)
                .NotEmpty().WithMessage("Strength is required.")
                .MaximumLength(50).WithMessage("Strength cannot exceed 50 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero.");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock Quantity cannot be negative.");
        }
    }
}
