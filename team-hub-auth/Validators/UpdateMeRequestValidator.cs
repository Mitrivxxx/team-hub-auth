using FluentValidation;
using team_hub_auth.Dtos;

namespace team_hub_auth.Validators;

public sealed class UpdateMeRequestValidator : AbstractValidator<UpdateMeRequest>
{
    public UpdateMeRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Surname is not null || x.Email is not null)
            .WithMessage("At least one field is required.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(name => HumanNameValidation.IsValid(name, 50))
            .WithMessage("FirstName must be 2-50 chars and contain only letters, single spaces, apostrophes, or hyphens.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Surname)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(surname => HumanNameValidation.IsValid(surname, 80))
            .WithMessage("LastName must be 2-80 chars and contain only letters, single spaces, apostrophes, or hyphens.")
            .When(x => x.Surname is not null);

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(EmailValidation.IsValid)
            .WithMessage("Email must be a valid email address.")
            .When(x => x.Email is not null);
    }
}
