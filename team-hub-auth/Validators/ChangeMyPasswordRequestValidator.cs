using FluentValidation;
using team_hub_auth.Dtos;

namespace team_hub_auth.Validators;

public sealed class ChangeMyPasswordRequestValidator : AbstractValidator<ChangeMyPasswordRequest>
{
    public ChangeMyPasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Length(8, 128)
            .WithMessage("Password must be 8-128 characters long.");
    }
}
