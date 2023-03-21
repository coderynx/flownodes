using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class GetClusterInfoHandler : IRequestHandler<GetClusterInfoRequest, GetClusterInfoResponse>
{
    private readonly IEnvironmentService _environmentService;

    public GetClusterInfoHandler(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    public Task<GetClusterInfoResponse> Handle(GetClusterInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = new GetClusterInfoResponse(_environmentService.ClusterId, _environmentService.ServiceId,
                _environmentService.SilosCount);
            return Task.FromResult(response);
        }
        catch
        {
            var response = new GetClusterInfoResponse(
                "There was an internal error while retrieving cluster information",
                ResponseKind.InternalError);
            return Task.FromResult(response);
        }
    }
}