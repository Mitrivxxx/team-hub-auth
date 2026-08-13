using Microsoft.Extensions.DependencyInjection;
using TeamHub.BlobStorage;
using team_hub_auth.Dtos;
using team_hub_auth.Models;

namespace team_hub_auth.Services.Users;

public interface IUserResponseMapper
{
    UserResponse Map(User user);
}

public sealed class UserResponseMapper(IServiceProvider services) : IUserResponseMapper
{
    public UserResponse Map(User user)
    {
        var blobStorage = services.GetService<IBlobStorageService>();
        return Map(user, blobStorage);
    }

    public static UserResponse Map(User user, IBlobStorageService? blobStorage) => new()
    {
        Id = user.Id,
        Username = user.Identity.Username,
        Email = user.Identity.Email,
        Name = user.Profile.Name,
        Surname = user.Profile.Surname,
        AvatarUrl = ResolveAvatarUrl(user.Profile.AvatarUrl, blobStorage)
    };

    public static string? ResolveAvatarUrl(string? blobPath, IBlobStorageService? blobStorage)
    {
        if (string.IsNullOrWhiteSpace(blobPath) || !BlobStoragePaths.IsUserAvatarPath(blobPath))
            return null;

        return blobStorage?.GetReadSasUri(blobPath)?.ToString();
    }
}
