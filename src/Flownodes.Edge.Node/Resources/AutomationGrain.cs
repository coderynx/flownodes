using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node.Models;
using Orleans;
using Orleans.Runtime;

namespace Flownodes.Edge.Node.Resources;

public class AutomationGrain : Grain, IAutomationGrain
{
    private readonly ILogger<AutomationGrain> _logger;
    private readonly IPersistentState<AutomationPersistence> _persistence;

    public AutomationGrain(ILogger<AutomationGrain> logger,
        [PersistentState("automationPersistence", "flownodes")]
        IPersistentState<AutomationPersistence> persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task ConfigureAsync()
    {
        _logger.LogInformation("Automation grain {AutomationGrainId} configured", Id);
    }

    public override Task OnActivateAsync()
    {
        _logger.LogInformation("Automation grain {AutomationGrainId} activated", Id);
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        _logger.LogInformation("Automation grain {AutomationGrainId} deactivated", Id);
        return base.OnDeactivateAsync();
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Executed automation of {AutomationGrainId} ", Id);
    }
}