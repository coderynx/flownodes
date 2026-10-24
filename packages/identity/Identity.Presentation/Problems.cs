using System.Security.Claims;
using Coderynx.Functional.Results.Errors;
using Identity.Application;
using Microsoft.AspNetCore.Http;

namespace Identity.Presentation;

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
                ErrorKind.Custom when error.Code == ErrorCodes.Unauthenticated => StatusCodes.Status401Unauthorized,
                ErrorKind.Custom when error.Code == ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            });

    public static Guid CallerId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}
