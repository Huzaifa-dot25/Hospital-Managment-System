using FluentValidation;
using Hospital.Application.DTOs.Auth;
using Hospital.Shared.Constants;

namespace Hospital.Application.Validations.Auth
{
    /// <summary>
    /// Validates the registration request.
    ///
    /// Role validation is important here:
    /// A malicious user must not be able to self-register as "SuperAdmin".
    /// We only allow certain roles to be self-assigned.
    /// Admin-level roles are assigned by existing admins, not during registration.
    /// </summary>
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        // Roles that can be assigned during self-registration.
        // Higher-privilege roles are assigned by an admin after account creation.
        private static readonly string[] AllowedSelfRegisterRoles =
        [
            Roles.Patient,
            Roles.Doctor,
            Roles.Nurse,
            Roles.Receptionist,
            Roles.Pharmacist,
            Roles.LabTechnician,
            Roles.Radiologist,
            Roles.Cashier,
            Roles.Accountant
        ];

        public RegisterDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.")
                .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
                .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"\d").WithMessage("Password must contain at least one number.")
                .Matches(@"[^a-zA-Z\d]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.")
                .Must(role => System.Array.Exists(AllowedSelfRegisterRoles,
                    r => r.Equals(role, System.StringComparison.OrdinalIgnoreCase)))
                .WithMessage($"Invalid role. Allowed roles: {string.Join(", ", AllowedSelfRegisterRoles)}");
        }
    }
}
