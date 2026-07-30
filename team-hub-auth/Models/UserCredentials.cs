namespace team_hub_auth.Models;

public sealed class UserCredentials
{
    // Stored as a hash in the `users.Password` column.
    public string PasswordHash { get; set; } = "";
}

