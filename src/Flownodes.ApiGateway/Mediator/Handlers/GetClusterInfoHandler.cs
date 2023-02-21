using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetClusterInfoHandler : IRequestHandler<GetClusterInfoRequest, GetClusterInfoResponse>
{
    private readonly IClusterGrain _cluster;

    public GetClusterInfoHandler(IGrainFactory grainFactory)
    {
        _cluster = grainFactory.GetGrain<IClusterGrain>(0);
    }

    public async Task<GetClusterInfoResponse> Handle(GetClusterInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var clusterInfo = await _cluster.GetClusterInformation();
            return new GetClusterInfoResponse(clusterInfo.ClusterId, clusterInfo.ServiceId, clusterInfo.NumberOfNodes);
        }
        catch
        {
            return new GetClusterInfoResponse("There was an internal error while retrieving cluster information");
        }
    }
}