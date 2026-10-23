using Coderynx.Functional.Results.Errors;
using Microsoft.AspNetCore.Http;

namespace Registry.Presentation.Namespaces;

internal static class Problems
{
    public static IResult ToProblem(this Error error) =>
        Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: error.Kind switch
            {
                ErrorKind.NotFound => StatusCodes.Status404NotFound,
                ErrorKind.Conflict => StatusCodes.Status409Conflict,
                ErrorKind.InvalidInput => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            });
}
