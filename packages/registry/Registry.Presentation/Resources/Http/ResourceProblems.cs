using Coderynx.Functional.Results.Errors;
using Core.Presentation.Http;
using Microsoft.AspNetCore.Http;
using Registry.Application.Resources;

namespace Registry.Presentation.Resources.Http;

internal static class ResourceProblems
{
    public static IResult ToResourceProblem(this Error error) =>
        error switch
        {
            ManifestRejected rejected => Problem(rejected, StatusCodes.Status422UnprocessableEntity, rejected.Problems),
            DeletionRefused refused => Problem(refused, StatusCodes.Status409Conflict, refused.Problems),
            _ => error.ToProblem()
        };

    private static IResult Problem(Error error, int status, IReadOnlyList<ManifestProblem> problems) =>
        Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?>
            {
                ["errors"] = problems.Select(problem => new
                    {
                        document = problem.Document,
                        kind = problem.Kind,
                        name = problem.Name,
                        path = problem.Path,
                        message = problem.Message
                    })
                    .ToList()
            });
}
