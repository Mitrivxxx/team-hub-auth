using Microsoft.EntityFrameworkCore;
using team_hub_auth.Data;
using team_hub_auth.Dtos;

namespace team_hub_auth.Services.Users;

public sealed class UserQueryService(AuthDbContext db) : IUserQueryService
{
    public async Task<IReadOnlyList<UserResponse>> GetUsersByIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return [];

        var uniqueIds = userIds.Distinct().ToArray();

        return await db.Users
            .AsNoTracking()
            .Where(u => uniqueIds.Contains(u.Id))
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                Name = u.Name,
                Surname = u.Surname
            })
            .ToListAsync(cancellationToken);
    }
}
