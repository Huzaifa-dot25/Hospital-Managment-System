using FluentValidation;
using Hospital.Application.DTOs.Billing;

namespace Hospital.Application.Validations
{
    public class CreateInvoiceDtoValidator : AbstractValidator<CreateInvoiceDto>
    {
        public CreateInvoiceDtoValidator()
        {
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("Patient ID is required.");

            RuleFor(x => x.TaxPercentage)
                .InclusiveBetween(0, 100).WithMessage("Tax percentage must be between 0 and 100.");

            RuleFor(x => x.DiscountAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Discount amount cannot be negative.");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("An invoice must contain at least one line item.");

            RuleForEach(x => x.Items).SetValidator(new CreateInvoiceItemDtoValidator());
        }
    }
}
