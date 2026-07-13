namespace team_hub_auth.Exceptions;

public sealed class RedisUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
