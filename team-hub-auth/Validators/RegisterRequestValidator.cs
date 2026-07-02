using FluentValidation;
using team_hub_auth.Dtos;

namespace team_hub_auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    static readonly string[] AllowedRoles = ["admin", "user"];

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(32)
            .Matches("^[a-zA-Z0-9_]+$")
            .WithMessage("Username may only contain letters, digits, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Surname)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);

        RuleFor(x => x.Role)
            .Must(role => role is null || AllowedRoles.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", AllowedRoles)}.");
    }
}
