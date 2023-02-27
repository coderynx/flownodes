using Flownodes.ApiGateway.Mediator.Responses;

namespace Flownodes.ApiGateway.Extensions;

public static class ResponseExtensions
{
    public static IResult GetResult(this Response response)
    {
        return response.ResponseKind switch
        {
            ResponseKind.Ok => Results.Ok(response),
            ResponseKind.NotFound => Results.NotFound(response),
            ResponseKind.InternalError => Results.BadRequest(response),
            ResponseKind.BadRequest => Results.BadRequest(response),
            _ => Results.NoContent()
        };
    }
}