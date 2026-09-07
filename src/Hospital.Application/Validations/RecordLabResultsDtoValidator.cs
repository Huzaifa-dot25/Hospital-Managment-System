using FluentValidation;
using Hospital.Application.DTOs.Laboratory;

namespace Hospital.Application.Validations
{
    public class RecordLabResultsDtoValidator : AbstractValidator<RecordLabResultsDto>
    {
        public RecordLabResultsDtoValidator()
        {
            RuleFor(x => x.Results)
                .NotEmpty().WithMessage("At least one test result must be provided.");

            RuleForEach(x => x.Results).ChildRules(item =>
            {
                item.RuleFor(r => r.LabItemId)
                    .NotEmpty().WithMessage("Lab item ID is required.");

                item.RuleFor(r => r.ResultValue)
                    .NotEmpty().WithMessage("Result value is required.")
                    .MaximumLength(500).WithMessage("Result value cannot exceed 500 characters.");

                item.RuleFor(r => r.Remarks)
                    .MaximumLength(1000).WithMessage("Remarks cannot exceed 1000 characters.");
            });
        }
    }
}
