using team_hub_auth.Dtos;

namespace team_hub_auth.Services.Users;

public interface IUserQueryService
{
    Task<IReadOnlyList<UserResponse>> GetAllUsersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetUsersByIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default);
}
