using FluentValidation;
using Hospital.Application.DTOs.Laboratory;

namespace Hospital.Application.Validations
{
    public class CreateLabOrderDtoValidator : AbstractValidator<CreateLabOrderDto>
    {
        public CreateLabOrderDtoValidator()
        {
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("Patient ID is required.");

            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("Doctor ID is required.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("A lab order must contain at least one test.");

            RuleForEach(x => x.Items).SetValidator(new CreateLabOrderItemDtoValidator());

            RuleFor(x => x.ClinicalNotes)
                .MaximumLength(1000).WithMessage("Clinical notes cannot exceed 1000 characters.");
        }
    }
}
