namespace Flownodes.Shared.Interfaces;

[GenerateSerializer]
public record ClusterInformation([property: Id(0)] string ClusterId, [property: Id(1)] string ServiceId, 
    [property: Id(2)] int NumberOfNodes, [property: Id(3)] string Version);

public interface IClusterGrain : IGrainWithIntegerKey
{
    ValueTask<ClusterInformation> GetClusterInformation();
}