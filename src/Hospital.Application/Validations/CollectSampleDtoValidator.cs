using FluentValidation;
using Hospital.Application.DTOs.Laboratory;

namespace Hospital.Application.Validations
{
    public class CollectSampleDtoValidator : AbstractValidator<CollectSampleDto>
    {
        public CollectSampleDtoValidator()
        {
            RuleFor(x => x.SampleCollectedBy)
                .MaximumLength(150).WithMessage("Sample collector name cannot exceed 150 characters.");
        }
    }
}
