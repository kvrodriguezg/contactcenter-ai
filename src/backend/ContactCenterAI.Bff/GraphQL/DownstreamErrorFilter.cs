using ContactCenterAI.Bff.Clients;
using HotChocolate;

namespace ContactCenterAI.Bff.GraphQL;

/// <summary>
/// Converts downstream failures into controlled GraphQL errors without stack traces.
/// </summary>
public static class DownstreamErrorHandling
{
    public static IError Map(IError error)
    {
        if (error.Exception is DownstreamApiException downstream)
        {
            return ErrorBuilder.New()
                .SetMessage(downstream.Message)
                .SetCode("DOWNSTREAM_UNAVAILABLE")
                .SetExtension("service", downstream.Service)
                .SetExtension("statusCode", downstream.StatusCode)
                .Build();
        }

        if (error.Exception is not null and not GraphQLException)
        {
            return ErrorBuilder.New()
                .SetMessage("Error interno del BFF.")
                .SetCode("INTERNAL")
                .Build();
        }

        // Strip exception details / stack traces from the payload.
        return ErrorBuilder.FromError(error)
            .SetException(null)
            .Build();
    }
}
