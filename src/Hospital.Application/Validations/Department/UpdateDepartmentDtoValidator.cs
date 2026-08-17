using FluentValidation;
using Hospital.Application.DTOs.Department;

namespace Hospital.Application.Validations.Department
{
    public class UpdateDepartmentDtoValidator : AbstractValidator<UpdateDepartmentDto>
    {
        public UpdateDepartmentDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Department ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Department name is required.")
                .MaximumLength(100).WithMessage("Department name must not exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
        }
    }
}
