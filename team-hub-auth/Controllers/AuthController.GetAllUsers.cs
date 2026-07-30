using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Dtos;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
    /// <summary>List users (paginated), optional search by name, surname, or email.</summary>
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
