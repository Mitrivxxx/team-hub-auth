using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Dtos;

namespace team_hub_auth.Controllers.Auth;

public partial class AuthController
{
    /// <summary>Search users by name, surname, or username (requires q, min 2 chars).</summary>
    [Authorize]
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? q = null,
        CancellationToken cancellationToken = default)
    {
        var users = await userQueryService.GetAllUsersAsync(page, pageSize, q, cancellationToken);
        return Ok(users);
    }
}
