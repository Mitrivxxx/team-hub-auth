using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TeamHub.BlobStorage;
using team_hub_auth.Exceptions;
using team_hub_auth.Models;
using team_hub_auth.Services.Users;
using team_hub_auth.Tests.Controllers;

namespace team_hub_auth.Tests.Services;

public class UserAvatarServiceTests
{
    [Fact]
    public async Task UploadAsync_WhenBlobStorageMissing_ThrowsUnavailable()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = AddUser(db);
        await db.SaveChangesAsync();
        var service = CreateService(db, blobStorage: null);

        await Assert.ThrowsAsync<AuthAvatarStorageUnavailableException>(
            () => service.UploadAsync(user.Id, CreateFile("image/png", 16)));
    }

    [Fact]
    public async Task UploadAsync_WhenContentTypeInvalid_ThrowsValidation()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = AddUser(db);
        await db.SaveChangesAsync();
        var service = CreateService(db, new FakeBlobStorageService());

        await Assert.ThrowsAsync<AuthAvatarValidationException>(
            () => service.UploadAsync(user.Id, CreateFile("image/gif", 16)));
    }

    [Fact]
    public async Task UploadAsync_WhenFileValid_StoresAvatarAndReturnsSasUrl()
    {
        await using var db = AuthControllerTestHelpers.CreateDbContext();
        var user = AddUser(db);
        await db.SaveChangesAsync();
        var blob = new FakeBlobStorageService();
        var service = CreateService(db, blob);

        var result = await service.UploadAsync(user.Id, CreateFile("image/png", 32));

        Assert.NotNull(result);
        Assert.Contains("users/", result!.AvatarUrl, StringComparison.Ordinal);
        Assert.Contains("sas=fake", result.AvatarUrl, StringComparison.Ordinal);
    }

    static User AddUser(team_hub_auth.Data.AuthDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Identity = new UserIdentity { Username = "alice", Email = "alice@example.com" },
            Profile = new UserProfile { Name = "Alice", Surname = "Smith" },
            Credentials = new UserCredentials { PasswordHash = "hash" }
        };
        db.Users.Add(user);
        return user;
    }

    static UserAvatarService CreateService(team_hub_auth.Data.AuthDbContext db, IBlobStorageService? blobStorage)
    {
        var services = new ServiceCollection();
        if (blobStorage is not null)
            services.AddSingleton(blobStorage);
        var provider = services.BuildServiceProvider();
        var mapper = new UserResponseMapper(provider);
        return new UserAvatarService(db, mapper, provider);
    }

    static IFormFile CreateFile(string contentType, int size)
    {
        var stream = new MemoryStream(new byte[size]);
        return new FormFile(stream, 0, size, "file", "avatar.bin")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    sealed class FakeBlobStorageService : IBlobStorageService
    {
        readonly Dictionary<string, byte[]> blobs = new(StringComparer.Ordinal);

        public Task UploadAsync(
            string blobName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default,
            string? downloadFileName = null)
        {
            using var memory = new MemoryStream();
            content.CopyTo(memory);
            blobs[blobName] = memory.ToArray();
            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(blobs[blobName]));

        public Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default)
        {
            blobs.Remove(blobName);
            return Task.CompletedTask;
        }

        public Uri? GetReadSasUri(string blobName) =>
            blobs.ContainsKey(blobName) ? new Uri($"https://blob.test/{blobName}?sas=fake") : null;
    }
}
