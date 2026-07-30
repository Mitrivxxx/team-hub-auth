using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Dtos;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
    /// <summary>List users (paginated).</summary>
    [Authorize]
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var users = await userQueryService.GetAllUsersAsync(page, pageSize, cancellationToken);
        return Ok(users);
    }
}
