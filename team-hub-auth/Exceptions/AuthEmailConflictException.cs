namespace team_hub_auth.Exceptions;

public sealed class AuthEmailConflictException : Exception
{
    public AuthEmailConflictException()
        : base("Email is already registered.")
    {
    }
}
