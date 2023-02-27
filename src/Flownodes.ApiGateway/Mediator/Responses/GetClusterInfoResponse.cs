namespace Flownodes.ApiGateway.Mediator.Responses;

public sealed record GetClusterInfoResponse : Response
{
    public GetClusterInfoResponse(string clusterName, string serviceName, int numberOfNodes)
    {
        ClusterName = clusterName;
        ServiceName = serviceName;
        NumberOfNodes = numberOfNodes;
    }

    public GetClusterInfoResponse(string message, ResponseKind responseKind) : base(message, responseKind)
    {
    }

    public string? ServiceName { get; }
    public string? ClusterName { get; }
    public int? NumberOfNodes { get; }
}