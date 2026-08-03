using team_hub_auth.Dtos;

namespace team_hub_auth.Services.Users;

public interface IUserQueryService
{
    Task<IReadOnlyList<UserResponse>> GetAllUsersAsync(
        int page = 1,
        int pageSize = 50,
        string? q = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetUsersByIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task<ResolveUsersResult> ResolveUsersAsync(
        IReadOnlyList<string> emails,
        IReadOnlyList<string> usernames,
        CancellationToken cancellationToken = default);
}

public sealed record ResolveUsersResult(
    IReadOnlyList<UserResponse> Users,
    IReadOnlyList<string> UnresolvedEmails,
    IReadOnlyList<string> UnresolvedUsernames);
