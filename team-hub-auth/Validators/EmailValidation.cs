using System.Net.Mail;

namespace team_hub_auth.Validators;

internal static class EmailValidation
{
    internal static bool IsValid(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length is < 3 or > 254)
            return false;

        try
        {
            _ = new MailAddress(normalized);
            return normalized.Contains('@', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
