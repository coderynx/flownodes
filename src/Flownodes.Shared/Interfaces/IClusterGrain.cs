namespace Flownodes.Shared.Interfaces;

[GenerateSerializer]
public record ClusterInformation([property: Id(0)] string ClusterId, [property: Id(1)] string ServiceId);

public interface IClusterGrain : IGrainWithIntegerKey
{
    ValueTask<ClusterInformation> GetClusterInformation();
}