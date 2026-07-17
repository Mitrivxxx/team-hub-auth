namespace team_hub_auth.Services.Sessions;

public sealed record RefreshSession(Guid UserId, bool RememberMe);
