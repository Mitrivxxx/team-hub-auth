using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using team_hub_auth.Configuration;

namespace team_hub_auth.Controllers;

[ApiController]
[ApiVersion(AuthApiVersions.Current)]
[Route("api/auth/v{version:apiVersion}")]
public abstract class AuthApiController : ControllerBase
{
    protected CreatedAtActionResult CreatedAtVersionedAction(string actionName, object routeValues, object? value)
    {
        var values = new RouteValueDictionary(routeValues)
        {
            ["version"] = AuthApiVersions.VersionMajor
        };
        return CreatedAtAction(actionName, values, value);
    }

    protected AcceptedAtActionResult AcceptedAtVersionedAction(string actionName, object routeValues, object? value)
    {
        var values = new RouteValueDictionary(routeValues)
        {
            ["version"] = AuthApiVersions.VersionMajor
        };
        return AcceptedAtAction(actionName, values, value);
    }
}
