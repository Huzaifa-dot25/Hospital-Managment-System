using FluentValidation;
using Hospital.Application.DTOs.Laboratory;

namespace Hospital.Application.Validations
{
    public class CreateLabTestDtoValidator : AbstractValidator<CreateLabTestDto>
    {
        public CreateLabTestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Lab test name is required.")
                .MaximumLength(150).WithMessage("Lab test name cannot exceed 150 characters.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Lab test code is required.")
                .MaximumLength(50).WithMessage("Lab test code cannot exceed 50 characters.");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required.")
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero.");

            RuleFor(x => x.TurnaroundTimeHours)
                .GreaterThan(0).WithMessage("Turnaround time must be greater than zero.");

            RuleFor(x => x.SampleType)
                .NotEmpty().WithMessage("Sample type is required.")
                .MaximumLength(100).WithMessage("Sample type cannot exceed 100 characters.");
        }
    }
}
