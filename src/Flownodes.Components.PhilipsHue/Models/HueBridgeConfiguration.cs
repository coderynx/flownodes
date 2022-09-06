using Refit;

namespace Flownodes.Components.PhilipsHue.Models;

public class HueBridgeConfiguration
{
    [AliasAs("name")] public string? Name { get; set; }

    [AliasAs("zigbeechannel")] public int? ZigbeeChannel { get; set; }

    [AliasAs("bridgeid")] public string? BridgeId { get; set; }

    [AliasAs("mac")] public string? MacAddress { get; set; }

    [AliasAs("dhcp")] public bool? IsDhcpEnabled { get; set; }

    [AliasAs("ipaddress")] public string? IpAddress { get; set; }

    [AliasAs("netmask")] public string? NetMask { get; set; }

    [AliasAs("gateway")] public string? Gateway { get; set; }

    [AliasAs("ProxyAddress")] public string? ProxyAddress { get; set; }

    [AliasAs("proxyport")] public int? ProxyPort { get; set; }

    [AliasAs("UTC")] public string? Utc { get; set; }

    [AliasAs("Localtime")] public string? LocalTime { get; set; }

    [AliasAs("timezone")] public string? TimeZone { get; set; }

    [AliasAs("modelid")] public string? Type { get; set; }

    [AliasAs("datastoreversion")] public string? DataStoreVersion { get; set; }

    [AliasAs("swversion")] public string? swversion { get; set; }

    [AliasAs("apiversion")] public string? ApiVersion { get; set; }
}