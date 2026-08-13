using TeamHub.Observability;
using TeamHub.Observability.Middleware;
using team_hub_auth.Exceptions;

namespace team_hub_auth.Configuration;

public sealed class AuthExceptionMapper : IExceptionProblemDetailsMapper
{
    public bool TryMap(Exception exception, out ExceptionMapping mapping)
    {
        switch (exception)
        {
            case AuthAvatarValidationException:
                mapping = new ExceptionMapping(
                    StatusCodes.Status400BadRequest,
                    "Bad request.",
                    exception.Message,
                    ProblemTypes.ValidationFailed,
                    PreferMappedDetail: true);
                return true;

            case AuthAvatarStorageUnavailableException:
                mapping = new ExceptionMapping(
                    StatusCodes.Status503ServiceUnavailable,
                    "Blob storage is not configured.",
                    exception.Message,
                    ProblemTypes.ServiceUnavailable,
                    PreferMappedDetail: true);
                return true;

            default:
                mapping = default;
                return false;
        }
    }
}
