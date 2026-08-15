using System.Diagnostics.Metrics;

namespace team_hub_auth.Observability;

public static class AuthMetrics
{
    public const string MeterName = "team-hub-auth";

    static readonly Meter Meter = new(MeterName);

    static readonly Counter<long> LoginFailures = Meter.CreateCounter<long>(
        "auth.login.failures",
        unit: "{attempt}",
        description: "Failed login attempts (invalid credentials).");

    static readonly Counter<long> LoginLockouts = Meter.CreateCounter<long>(
        "auth.login.lockouts",
        unit: "{lockout}",
        description: "Login responses blocked by account lockout.");

    static readonly Counter<long> Registrations = Meter.CreateCounter<long>(
        "auth.registrations",
        unit: "{user}",
        description: "Successful user registrations.");

    public static void RecordLoginFailure() => LoginFailures.Add(1);

    public static void RecordLoginLockout() => LoginLockouts.Add(1);

    public static void RecordRegistration() => Registrations.Add(1);
}
