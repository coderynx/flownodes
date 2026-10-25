using Coderynx.Functional.Results.Errors;
using Core.Presentation.Http;
using Microsoft.AspNetCore.Http;
using Registry.Application.Resources;

namespace Registry.Presentation.Resources.Contracts;

internal static class ResourceProblems
{
    public static IResult ToResourceProblem(this Error error) =>
        error is ManifestRejected rejected
            ? Results.Problem(
                title: rejected.Code,
                detail: rejected.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = rejected.Problems.Select(problem => new
                        {
                            document = problem.Document,
                            kind = problem.Kind,
                            name = problem.Name,
                            path = problem.Path,
                            message = problem.Message
                        })
                        .ToList()
                })
            : error.ToProblem();
}
