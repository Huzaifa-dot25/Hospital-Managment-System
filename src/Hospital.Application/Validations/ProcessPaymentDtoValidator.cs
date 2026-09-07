using FluentValidation;
using Hospital.Application.DTOs.Billing;

namespace Hospital.Application.Validations
{
    public class ProcessPaymentDtoValidator : AbstractValidator<ProcessPaymentDto>
    {
        public ProcessPaymentDtoValidator()
        {
            RuleFor(x => x.InvoiceId)
                .NotEmpty().WithMessage("Invoice ID is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Payment amount must be greater than zero.");

            RuleFor(x => x.TransactionReference)
                .MaximumLength(100).WithMessage("Transaction reference cannot exceed 100 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
        }
    }
}
