namespace team_hub_auth.Dtos;

public sealed class AuthLoginErrorResponse
{
    public string Code { get; init; } = "";

    public int? RemainingAttempts { get; init; }

    public int? LockoutSeconds { get; init; }
}

