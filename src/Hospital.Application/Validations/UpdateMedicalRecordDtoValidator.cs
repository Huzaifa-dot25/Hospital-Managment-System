using FluentValidation;
using Hospital.Application.DTOs.MedicalRecord;

namespace Hospital.Application.Validations
{
    public class UpdateMedicalRecordDtoValidator : AbstractValidator<UpdateMedicalRecordDto>
    {
        public UpdateMedicalRecordDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Medical Record ID is required.");

            RuleFor(x => x.Diagnosis)
                .NotEmpty().WithMessage("Diagnosis is required.")
                .MaximumLength(200).WithMessage("Diagnosis cannot exceed 200 characters.");

            RuleFor(x => x.Symptoms)
                .NotEmpty().WithMessage("Symptoms are required.")
                .MaximumLength(2000).WithMessage("Symptoms cannot exceed 2000 characters.");

            RuleFor(x => x.Treatment)
                .NotEmpty().WithMessage("Treatment plan is required.")
                .MaximumLength(2000).WithMessage("Treatment plan cannot exceed 2000 characters.");

            RuleFor(x => x.Prescription)
                .MaximumLength(2000).WithMessage("Prescription cannot exceed 2000 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.");
        }
    }
}
