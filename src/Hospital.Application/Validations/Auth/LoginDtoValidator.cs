using FluentValidation;
using Hospital.Application.DTOs.Auth;

namespace Hospital.Application.Validations.Auth
{
    /// <summary>
    /// Validates the login request before it hits the service layer.
    ///
    /// Why validate here and not in the AuthService?
    /// Consistency. Every DTO in the system is validated the same way —
    /// FluentValidation catches bad input before any business logic runs.
    ///
    /// Security note: we keep error messages vague on purpose.
    /// "Email is required" is fine — it doesn't reveal system internals.
    /// We never say "no user with that email exists" — that leaks user enumeration data.
    /// </summary>
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
        }
    }
}
