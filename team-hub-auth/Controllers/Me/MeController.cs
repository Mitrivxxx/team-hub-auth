using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Observability;
using team_hub_auth.Dtos;
using team_hub_auth.Exceptions;
using team_hub_auth.Services;
using team_hub_auth.Services.Users;

namespace team_hub_auth.Controllers.Me;

[Authorize]
public sealed class MeController(
    ICurrentUserService currentUserService,
    IMeProfileService userProfileService,
    IUserAvatarService userAvatarService,
    ILogger<MeController> logger) : AuthApiController
{
    const string RefreshTokenCookieName = "refreshToken";
    const string RefreshTokenPersistentCookieName = "refreshTokenPersistent";

    /// <summary>Get the current user profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken = default)
    {
        if (!currentUserService.TryGetUserId(out var userId))
            return UnauthorizedProblem("User is not authenticated.");

        var user = await userProfileService.GetAsync(userId, cancellationToken);
        return user is null
            ? UnauthorizedProblem("User was not found.")
            : Ok(user);
    }

    /// <summary>Update the current user profile.</summary>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMe(UpdateMeRequest request, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.TryGetUserId(out var userId))
            return UnauthorizedProblem("User is not authenticated.");

        try
        {
            var result = await userProfileService.UpdateAsync(userId, request, cancellationToken);
            if (result is null)
                return UnauthorizedProblem("User was not found.");

            if (result.RequiresReauth)
                DeleteRefreshTokenCookie();

            logger.LogInformation("User {UserId} updated profile", userId);
            return Ok(result.User);
        }
        catch (AuthEmailConflictException)
        {
            logger.LogWarning("Profile update failed for user {UserId}: email already registered", userId);
            return TeamHubProblemDetailsFactory.ObjectResult(
                TeamHubProblemDetailsFactory.CreateValidation(
                    HttpContext,
                    new Dictionary<string, string[]>
                    {
                        ["email"] = ["Email is already registered."]
                    },
                    StatusCodes.Status409Conflict,
                    "Profile update failed.",
                    detail: null,
                    ProblemTypes.Conflict));
        }
    }

    /// <summary>Change password for the current user.</summary>
    [HttpPost("me/change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeMyPassword(
        ChangeMyPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserService.TryGetUserId(out var userId))
            return UnauthorizedProblem("User is not authenticated.");

        var changed = await userProfileService.ChangePasswordAsync(userId, request, cancellationToken);
        if (!changed)
        {
            logger.LogWarning("Password change failed for user {UserId}", userId);
            return UnauthorizedProblem("Current password is invalid.");
        }

        DeleteRefreshTokenCookie();
        logger.LogInformation("User {UserId} changed password successfully", userId);
        return NoContent();
    }

    /// <summary>Upload the current user avatar.</summary>
    [HttpPut("me/avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.TryGetUserId(out var userId))
            return UnauthorizedProblem("User is not authenticated.");

        var user = await userAvatarService.UploadAsync(userId, file, cancellationToken);
        if (user is null)
            return UnauthorizedProblem("User was not found.");

        logger.LogInformation("User {UserId} uploaded avatar", userId);
        return Ok(user);
    }

    /// <summary>Delete the current user avatar.</summary>
    [HttpDelete("me/avatar")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken = default)
    {
        if (!currentUserService.TryGetUserId(out var userId))
            return UnauthorizedProblem("User is not authenticated.");

        var user = await userAvatarService.DeleteAsync(userId, cancellationToken);
        if (user is null)
            return UnauthorizedProblem("User was not found.");

        logger.LogInformation("User {UserId} deleted avatar", userId);
        return Ok(user);
    }

    void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
        Response.Cookies.Delete(RefreshTokenPersistentCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    IActionResult UnauthorizedProblem(string detail) =>
        TeamHubProblemDetailsFactory.ObjectResult(TeamHubProblemDetailsFactory.Create(
            HttpContext,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            detail,
            ProblemTypes.Unauthorized));
}
