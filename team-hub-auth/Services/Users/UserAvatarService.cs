using Microsoft.EntityFrameworkCore;
using TeamHub.BlobStorage;
using team_hub_auth.Data;
using team_hub_auth.Dtos;
using team_hub_auth.Exceptions;

namespace team_hub_auth.Services.Users;

public interface IUserAvatarService
{
    Task<UserResponse?> UploadAsync(Guid userId, IFormFile? file, CancellationToken cancellationToken = default);
    Task<UserResponse?> DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class UserAvatarService(
    AuthDbContext db,
    IUserResponseMapper userResponseMapper,
    IServiceProvider serviceProvider) : IUserAvatarService
{
    const long MaxFileSizeBytes = 2 * 1024 * 1024;

    static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp"
    };

    IBlobStorageService? BlobStorage => serviceProvider.GetService<IBlobStorageService>();

    public async Task<UserResponse?> UploadAsync(
        Guid userId,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        var blobStorage = BlobStorage ?? throw new AuthAvatarStorageUnavailableException();

        ValidateFile(file);
        var avatarFile = file!;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return null;

        var extension = AllowedContentTypes[avatarFile.ContentType];
        var blobName = BlobStoragePaths.UserAvatar(userId, extension);

        if (!string.IsNullOrWhiteSpace(user.Profile.AvatarUrl)
            && !string.Equals(user.Profile.AvatarUrl, blobName, StringComparison.Ordinal))
        {
            await blobStorage.DeleteIfExistsAsync(user.Profile.AvatarUrl, cancellationToken);
        }

        await using var stream = avatarFile.OpenReadStream();
        await blobStorage.UploadAsync(blobName, stream, avatarFile.ContentType, cancellationToken);

        user.Profile.AvatarUrl = blobName;
        await db.SaveChangesAsync(cancellationToken);

        return userResponseMapper.Map(user);
    }

    public async Task<UserResponse?> DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var blobStorage = BlobStorage ?? throw new AuthAvatarStorageUnavailableException();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return null;

        if (!string.IsNullOrWhiteSpace(user.Profile.AvatarUrl))
        {
            await blobStorage.DeleteIfExistsAsync(user.Profile.AvatarUrl, cancellationToken);
            user.Profile.AvatarUrl = null;
            await db.SaveChangesAsync(cancellationToken);
        }

        return userResponseMapper.Map(user);
    }

    static void ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new AuthAvatarValidationException("Avatar file is required.");

        if (file.Length > MaxFileSizeBytes)
            throw new AuthAvatarValidationException("Avatar file must be 2 MB or smaller.");

        if (!AllowedContentTypes.ContainsKey(file.ContentType))
            throw new AuthAvatarValidationException("Avatar must be a JPEG, PNG, or WebP image.");
    }
}
