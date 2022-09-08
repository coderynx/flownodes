using Refit;

namespace Flownodes.Components.PhilipsHue.Models;

internal record HueLight
{
    [AliasAs("id")] public string? Type { get; set; }

    [AliasAs("name")] public string? Name { get; set; }

    [AliasAs("modelid")] public string? ModelId { get; set; }

    [AliasAs("manufacturername")] public string? ManufacturerName { get; set; }

    [AliasAs("productname")] public string? ProductName { get; set; }

    [AliasAs("uniqueid")] public string? UniqueId { get; set; }

    [AliasAs("swversion")] public string? SoftwareVersion { get; set; }

    [AliasAs("swconfigid")] public string? SoftwareConfigId { get; set; }

    [AliasAs("productid")] public string? ProductId { get; set; }

    [AliasAs("state")] public HueLightState? State { get; set; }
}

internal record HueLightConfiguration
{
    [AliasAs("archetype")] public string? Archetype { get; set; }

    [AliasAs("function")] public string? Function { get; set; }

    [AliasAs("direction")] public string? Direction { get; set; }

    [AliasAs("startup")] public HueLightStartup? Startup { get; set; }
}

internal record HueLightStartup
{
    [AliasAs("mode")] public string? Mode { get; set; }

    [AliasAs("configured")] public string? IsConfigured { get; set; }
}

internal record HueLightState
{
    [AliasAs("on")] public bool? IsOn { get; set; }

    [AliasAs("alert")] public string? Alert { get; set; }

    [AliasAs("mode")] public string? Mode { get; set; }

    [AliasAs("reachable")] public bool? IsReachable { get; set; }
}