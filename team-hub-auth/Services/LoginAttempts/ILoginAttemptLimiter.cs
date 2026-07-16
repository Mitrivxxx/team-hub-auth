namespace team_hub_auth.Services.LoginAttempts;

public interface ILoginAttemptLimiter
{
    Task<LoginLockoutStatus> GetLockoutStatusAsync(string username, CancellationToken cancellationToken = default);

    Task<LoginFailureOutcome> RegisterFailedAttemptAsync(
        string username,
        int maxFailedAttempts,
        TimeSpan lockoutDuration,
        CancellationToken cancellationToken = default);

    Task ClearAttemptsAsync(string username, CancellationToken cancellationToken = default);
}

public sealed record LoginLockoutStatus(bool IsLocked, int LockoutSeconds);

public sealed record LoginFailureOutcome(int RemainingAttempts, bool IsLocked, int LockoutSeconds);

