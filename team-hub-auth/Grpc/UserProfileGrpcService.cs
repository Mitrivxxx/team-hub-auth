using Grpc.Core;
using TeamHub.GrpcContracts.Auth.V1;
using team_hub_auth.Services.Users;

namespace team_hub_auth.Grpc;

public sealed class UserProfileGrpcService(IUserQueryService userQueryService) : UserProfileService.UserProfileServiceBase
{
    /// <summary>Return user profiles for the given ids.</summary>
    public override async Task<GetUsersByIdsResponse> GetUsersByIds(
        GetUsersByIdsRequest request,
        ServerCallContext context)
    {
        var ids = new List<Guid>(request.UserIds.Count);
        foreach (var rawId in request.UserIds)
        {
            if (!Guid.TryParse(rawId, out var userId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid user id: {rawId}"));

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

        return response;
    }

    /// <summary>Resolve user profiles by email and/or username.</summary>
    public override async Task<ResolveUsersResponse> ResolveUsers(
        ResolveUsersRequest request,
        ServerCallContext context)
    {
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
        return response;
    }
}
