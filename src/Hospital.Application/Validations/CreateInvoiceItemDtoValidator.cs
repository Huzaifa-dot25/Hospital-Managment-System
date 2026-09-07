using FluentValidation;
using Hospital.Application.DTOs.Billing;

namespace Hospital.Application.Validations
{
    public class CreateInvoiceItemDtoValidator : AbstractValidator<CreateInvoiceItemDto>
    {
        public CreateInvoiceItemDtoValidator()
        {
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Item description is required.")
                .MaximumLength(250).WithMessage("Description cannot exceed 250 characters.");

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0).WithMessage("Unit price must be greater than zero.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be at least 1.");
        }
    }
}
