using Microsoft.EntityFrameworkCore;
using team_hub_auth.Data;
using team_hub_auth.Dtos;

namespace team_hub_auth.Services.Users;

public sealed class UserQueryService(AuthDbContext db) : IUserQueryService
{
    const int DefaultPageSize = 50;
    const int MaxPageSize = 100;

    public async Task<IReadOnlyList<UserResponse>> GetAllUsersAsync(
        int page = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        return await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Identity.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Identity.Username,
                Email = u.Identity.Email,
                Name = u.Profile.Name,
                Surname = u.Profile.Surname
            })
            .ToListAsync(cancellationToken);
    }

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
                Username = u.Identity.Username,
                Email = u.Identity.Email,
                Name = u.Profile.Name,
                Surname = u.Profile.Surname
            })
            .ToListAsync(cancellationToken);
    }
}
