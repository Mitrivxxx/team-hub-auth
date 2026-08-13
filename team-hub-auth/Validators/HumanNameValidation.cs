using System.Text.RegularExpressions;

namespace team_hub_auth.Validators;

internal static class HumanNameValidation
{
    static readonly Regex HumanNameRegex = new(
        @"^\p{L}+(?:[ '-]\p{L}+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    internal static bool IsValid(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length >= 2
            && normalized.Length <= maxLength
            && HumanNameRegex.IsMatch(normalized);
    }
}
