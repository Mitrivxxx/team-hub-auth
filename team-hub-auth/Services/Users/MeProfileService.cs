using Microsoft.EntityFrameworkCore;
using team_hub_auth.Data;
using team_hub_auth.Dtos;
using team_hub_auth.Exceptions;
using team_hub_auth.Services.Password;
using team_hub_auth.Services.Sessions;

namespace team_hub_auth.Services.Users;

public interface IMeProfileService
{
    Task<UserResponse?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MeProfileUpdateResult?> UpdateAsync(Guid userId, UpdateMeRequest request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, ChangeMyPasswordRequest request, CancellationToken cancellationToken = default);
}

public sealed record MeProfileUpdateResult(UserResponse User, bool RequiresReauth);

public sealed class MeProfileService(
    AuthDbContext db,
    IPasswordHasher passwordHasher,
    IUserResponseMapper userResponseMapper,
    ISessionStore sessionStore) : IMeProfileService
{
    public async Task<UserResponse?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is null ? null : userResponseMapper.Map(user);
    }

    public async Task<MeProfileUpdateResult?> UpdateAsync(
        Guid userId,
        UpdateMeRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return null;

        var emailChanged = false;

        if (request.Name is not null)
            user.Profile.Name = request.Name.Trim();

        if (request.Surname is not null)
            user.Profile.Surname = request.Surname.Trim();

        if (request.Email is not null)
        {
            var email = request.Email.Trim();
            var loweredEmail = email.ToLowerInvariant();
            if (!string.Equals(user.Identity.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await db.Users.AnyAsync(
                    u => u.Id != userId && u.Identity.Email.ToLower() == loweredEmail,
                    cancellationToken);
                if (emailTaken)
                    throw new AuthEmailConflictException();

                user.Identity.Email = email;
                emailChanged = true;
            }
            else
            {
                user.Identity.Email = email;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (emailChanged)
            await sessionStore.RevokeAllSessionsAsync(userId, cancellationToken);

        return new MeProfileUpdateResult(userResponseMapper.Map(user), emailChanged);
    }

    public async Task<bool> ChangePasswordAsync(
        Guid userId,
        ChangeMyPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return false;

        if (!passwordHasher.Verify(request.CurrentPassword, user.Credentials.PasswordHash))
            return false;

        user.Credentials.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.Security.FailedLoginAttempts = 0;
        user.Security.LockoutUntil = null;
        await db.SaveChangesAsync(cancellationToken);
        await sessionStore.RevokeAllSessionsAsync(userId, cancellationToken);
        return true;
    }
}
