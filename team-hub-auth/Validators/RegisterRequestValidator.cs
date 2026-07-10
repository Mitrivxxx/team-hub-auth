using FluentValidation;
using System.Text.RegularExpressions;
using team_hub_auth.Dtos;

namespace team_hub_auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    static readonly Regex HumanNameRegex = new(
        @"^\p{L}+(?:[ '-]\p{L}+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(name => IsValidHumanName(name, 50))
            .WithMessage("FirstName must be 2-50 chars and contain only letters, single spaces, apostrophes, or hyphens.");

        RuleFor(x => x.Surname)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(surname => IsValidHumanName(surname, 80))
            .WithMessage("LastName must be 2-80 chars and contain only letters, single spaces, apostrophes, or hyphens.");

        RuleFor(x => x.Username)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(UsernameValidation.IsValid)
            .WithMessage("Username must be 3-30 chars and contain only letters, digits, dot, underscore, or hyphen.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Length(12, 128)
            .WithMessage("Password must be 12-128 characters long.");
    }

    static bool IsValidHumanName(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length >= 2
            && normalized.Length <= maxLength
            && HumanNameRegex.IsMatch(normalized);
    }

}
