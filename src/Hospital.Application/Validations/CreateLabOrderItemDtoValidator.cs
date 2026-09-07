using FluentValidation;
using Hospital.Application.DTOs.Laboratory;

namespace Hospital.Application.Validations
{
    public class CreateLabOrderItemDtoValidator : AbstractValidator<CreateLabOrderItemDto>
    {
        public CreateLabOrderItemDtoValidator()
        {
            RuleFor(x => x.LabTestId)
                .NotEmpty().WithMessage("Lab test ID is required.");
        }
    }
}
