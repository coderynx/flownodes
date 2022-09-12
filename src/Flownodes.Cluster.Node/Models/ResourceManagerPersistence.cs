namespace Flownodes.Cluster.Node.Models;

public class ResourceManagerPersistence
{
    public Dictionary<string, string> DeviceRegistration { get; set; } = new();
    public Dictionary<string, string> DataCollectorRegistrations { get; set; } = new();
    public List<string> AssetRegistrations { get; set; } = new();
}