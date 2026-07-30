using team_hub_auth.Dtos;

namespace team_hub_auth.Services.Users;

public interface IUserQueryService
{
    Task<IReadOnlyList<UserResponse>> GetAllUsersAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetUsersByIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default);
}
