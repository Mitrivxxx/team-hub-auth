using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Dtos;

namespace team_hub_auth.Controllers;

public partial class AuthController
{
    /// <summary>List all users.</summary>
    [Authorize]
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await userQueryService.GetAllUsersAsync(cancellationToken);
        return Ok(users);
    }
}
