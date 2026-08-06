using TeamHub.Observability;
using TeamHub.Observability.Middleware;
using team_hub_auth.Exceptions;

namespace team_hub_auth.Configuration;

public sealed class RedisUnavailableExceptionMapper : IExceptionProblemDetailsMapper
{
    const string Detail = "Authentication service temporarily unavailable. Please try again later.";

    public bool TryMap(Exception exception, out ExceptionMapping mapping)
    {
        if (exception is not RedisUnavailableException)
        {
            mapping = default;
            return false;
        }

        mapping = new ExceptionMapping(
            StatusCodes.Status503ServiceUnavailable,
            "Authentication service temporarily unavailable",
            Detail,
            ProblemTypes.ServiceUnavailable,
            PreferMappedDetail: true);
        return true;
    }
}
