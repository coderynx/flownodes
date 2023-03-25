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

    public async Task<GetClusterInfoResponse> Handle(GetClusterInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userManager = _environmentService.GetUserManager();

            ClusterStatus clusterStatus;
            if (await userManager.HasUsers()) clusterStatus = ClusterStatus.Ready;
            else clusterStatus = ClusterStatus.Uninitialized;

            return new GetClusterInfoResponse(_environmentService.ClusterId, _environmentService.ServiceId, _environmentService.SilosCount, clusterStatus);
        }
        catch
        {
            var response = new GetClusterInfoResponse(
                "There was an internal error while retrieving cluster information",
                ResponseKind.InternalError);
            return response;
        }
    }
}