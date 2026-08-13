namespace team_hub_auth.Exceptions;

public sealed class AuthAvatarStorageUnavailableException : Exception
{
    public AuthAvatarStorageUnavailableException()
        : base("Blob storage is not configured.")
    {
    }
}
