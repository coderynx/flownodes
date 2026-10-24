using System.Security.Claims;
using Coderynx.Functional.Results.Errors;
using Core.Application.Authorization;
using Microsoft.AspNetCore.Http;

namespace Core.Presentation.Http;

/// <summary>
/// Translates a handler's outcome and the caller's token into HTTP, the same way in every module (ADR-0020).
/// </summary>
public static class Problems
{
    private const int Unauthorized = StatusCodes.Status401Unauthorized;
    private const int Forbidden = StatusCodes.Status403Forbidden;

    /// <summary>
    /// Maps an error to an <c>application/problem+json</c> response: 400 for invalid input, 401 for an unauthenticated
    /// caller, 403 for a missing role, 404 for something absent or out of the caller's sight, 409 for a conflict and
    /// 500 for anything else.
    /// </summary>
    /// <param name="error">The error a handler returned.</param>
    /// <returns>The problem response.</returns>
    public static IResult ToProblem(this Error error) =>
        Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: error.Kind switch
            {
                ErrorKind.NotFound => StatusCodes.Status404NotFound,
                ErrorKind.Conflict => StatusCodes.Status409Conflict,
                ErrorKind.InvalidInput => StatusCodes.Status400BadRequest,
                ErrorKind.Custom when error.Code == AccessErrors.UnauthenticatedCode => Unauthorized,
                ErrorKind.Custom when error.Code == AccessErrors.ForbiddenCode => Forbidden,
                _ => StatusCodes.Status500InternalServerError
            });

    /// <summary>
    /// Reads the User id from the subject of the caller's access token.
    /// </summary>
    /// <param name="principal">The caller, as authenticated from an access token.</param>
    /// <returns>The User id, or an empty id when the token carries none.</returns>
    public static Guid CallerId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}
