using Engine.Application.Integrations;
using Engine.Domain.Integrations;

namespace Flowctl.IntegrationTests;

/// <summary>
/// The Integrations the test Silo found: one that loaded and one that did not. Loading them is the Engine's own
/// tests' business.
/// </summary>
public sealed class InstalledIntegrationsStub : IInstalledIntegrations
{
    public IReadOnlyList<InstalledIntegration> All { get; } = Build();

    private static InstalledIntegration[] Build()
    {
        IntegrationId.TryCreate("flownodes.simulator", out var id);

        return
        [
            InstalledIntegration.Failed(
                "legacy",
                IntegrationLoadFailure.IncompatibleSdk,
                "Built against SDK 0.9.0.",
                null,
                "0.9.0"),
            InstalledIntegration.Loaded("simulator", id!, "0.1.0", "0.1.0", ["Thermometer", "Switch"])
        ];
    }
}
