using Grpc.Core;
using TeamHub.GrpcContracts.Auth.V1;
using team_hub_auth.Services.Users;

namespace team_hub_auth.Grpc;

public sealed class UserProfileGrpcService(
    IUserQueryService userQueryService,
    ILogger<UserProfileGrpcService> logger) : UserProfileService.UserProfileServiceBase
{
    /// <summary>Return user profiles for the given ids.</summary>
    public override async Task<GetUsersByIdsResponse> GetUsersByIds(
        GetUsersByIdsRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("gRPC GetUsersByIds for {UserIdCount} ids", request.UserIds.Count);

        var ids = new List<Guid>(request.UserIds.Count);
        foreach (var rawId in request.UserIds)
        {
            if (!Guid.TryParse(rawId, out var userId))
            {
                logger.LogWarning("gRPC GetUsersByIds rejected invalid user id {UserId}", rawId);
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid user id: {rawId}"));
            }

            ids.Add(userId);
        }

        var users = await userQueryService.GetUsersByIdsAsync(ids, context.CancellationToken);

        var response = new GetUsersByIdsResponse();
        response.Users.AddRange(users.Select(u => new UserProfile
        {
            Id = u.Id.ToString(),
            Username = u.Username,
            Email = u.Email,
            Name = u.Name,
            Surname = u.Surname
        }));

        logger.LogInformation("gRPC GetUsersByIds returned {UserCount} profiles", response.Users.Count);
        return response;
    }

    /// <summary>Resolve user profiles by email and/or username.</summary>
    public override async Task<ResolveUsersResponse> ResolveUsers(
        ResolveUsersRequest request,
        ServerCallContext context)
    {
        logger.LogInformation(
            "gRPC ResolveUsers emails={EmailCount} usernames={UsernameCount}",
            request.Emails.Count,
            request.Usernames.Count);

        var result = await userQueryService.ResolveUsersAsync(
            request.Emails.ToList(),
            request.Usernames.ToList(),
            context.CancellationToken);

        var response = new ResolveUsersResponse();
        response.Users.AddRange(result.Users.Select(u => new UserProfile
        {
            Id = u.Id.ToString(),
            Username = u.Username,
            Email = u.Email,
            Name = u.Name,
            Surname = u.Surname
        }));
        response.UnresolvedEmails.AddRange(result.UnresolvedEmails);
        response.UnresolvedUsernames.AddRange(result.UnresolvedUsernames);

        logger.LogInformation(
            "gRPC ResolveUsers returned {UserCount} profiles ({UnresolvedEmailCount} unresolved emails, {UnresolvedUsernameCount} unresolved usernames)",
            response.Users.Count,
            response.UnresolvedEmails.Count,
            response.UnresolvedUsernames.Count);
        return response;
    }
}
