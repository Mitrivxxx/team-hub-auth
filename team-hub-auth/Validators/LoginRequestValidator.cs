using FluentValidation;
using team_hub_auth.Dtos;

namespace team_hub_auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(UsernameValidation.IsValid)
            .WithMessage("Username must be 3-30 chars and contain only letters, digits, dot, underscore, or hyphen.");

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
