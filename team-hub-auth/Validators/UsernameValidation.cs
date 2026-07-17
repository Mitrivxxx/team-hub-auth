using System.Text.RegularExpressions;

namespace team_hub_auth.Validators;

internal static class UsernameValidation
{
    static readonly Regex UsernameRegex = new(
        @"^[a-zA-Z0-9._-]{3,30}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    internal static bool IsValid(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return UsernameRegex.IsMatch(normalized);
    }
}
