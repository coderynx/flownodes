using Flownodes.ApiGateway.Mediator.Responses;

namespace Flownodes.ApiGateway.Extensions;

public static class ResponseExtensions
{
    public static IResult GetResult(this Response response)
    {
        return response.IsSuccess is false ? Results.NotFound(response) : Results.Ok(response);
    }
}