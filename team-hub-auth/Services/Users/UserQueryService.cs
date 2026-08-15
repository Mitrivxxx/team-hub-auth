using Microsoft.EntityFrameworkCore;
using team_hub_auth.Data;
using team_hub_auth.Dtos;

namespace team_hub_auth.Services.Users;

public sealed class UserQueryService(AuthDbContext db) : IUserQueryService
{
    const int DefaultPageSize = 50;
    const int MaxPageSize = 100;
    const int MinSearchQueryLength = 2;

    public async Task<IReadOnlyList<UserResponse>> GetAllUsersAsync(
        int page = 1,
        int pageSize = DefaultPageSize,
        string? q = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var term = q?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < MinSearchQueryLength)
            return [];

        var query = db.Users.AsNoTracking();

        var pattern = term.ToLowerInvariant();
        var tokens = pattern
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length <= 1)
        {
            query = query.Where(u =>
                u.Profile.Name.ToLower().Contains(pattern) ||
                u.Profile.Surname.ToLower().Contains(pattern) ||
                u.Identity.Username.ToLower().Contains(pattern));
        }
        else
        {
            // "Jan Kowalski" — each token must match name, surname, or username
            foreach (var token in tokens)
            {
                var t = token;
                query = query.Where(u =>
                    u.Profile.Name.ToLower().Contains(t) ||
                    u.Profile.Surname.ToLower().Contains(t) ||
                    u.Identity.Username.ToLower().Contains(t));
            }
        }

        return await query
            .OrderBy(u => u.Identity.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Identity.Username,
                Email = "",
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

    public async Task<ResolveUsersResult> ResolveUsersAsync(
        IReadOnlyList<string> emails,
        IReadOnlyList<string> usernames,
        CancellationToken cancellationToken = default)
    {
        var emailKeys = NormalizeKeys(emails);
        var usernameKeys = NormalizeKeys(usernames);

        if (emailKeys.Count == 0 && usernameKeys.Count == 0)
            return new ResolveUsersResult([], [], []);

        var users = await db.Users
            .AsNoTracking()
            .Where(u =>
                emailKeys.Contains(u.Identity.Email.ToLower())
                || usernameKeys.Contains(u.Identity.Username.ToLower()))
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Identity.Username,
                Email = u.Identity.Email,
                Name = u.Profile.Name,
                Surname = u.Profile.Surname
            })
            .ToListAsync(cancellationToken);

        var matchedEmails = users
            .Select(u => u.Email.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        var matchedUsernames = users
            .Select(u => u.Username.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var unresolvedEmails = emailKeys.Where(e => !matchedEmails.Contains(e)).ToList();
        var unresolvedUsernames = usernameKeys.Where(u => !matchedUsernames.Contains(u)).ToList();

        return new ResolveUsersResult(users, unresolvedEmails, unresolvedUsernames);
    }

    static List<string> NormalizeKeys(IReadOnlyList<string> values) =>
        values
            .Select(v => v?.Trim().ToLowerInvariant())
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToList();
}
