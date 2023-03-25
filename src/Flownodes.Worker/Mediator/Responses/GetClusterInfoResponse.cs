namespace Flownodes.Worker.Mediator.Responses;

public enum ClusterStatus
{
    Uninitialized,
    Ready,
    Error,
    Unknown
}

public sealed record GetClusterInfoResponse : Response
{
    public GetClusterInfoResponse(string clusterName, string serviceName, int numberOfNodes, ClusterStatus status)
    {
        ClusterName = clusterName;
        ServiceName = serviceName;
        NumberOfNodes = numberOfNodes;
        Status = status;
    }

    public GetClusterInfoResponse(string message, ResponseKind responseKind) : base(message, responseKind)
    {
    }

    public ClusterStatus? Status { get; }
    public string? ServiceName { get; }
    public string? ClusterName { get; }
    public int? NumberOfNodes { get; }
}