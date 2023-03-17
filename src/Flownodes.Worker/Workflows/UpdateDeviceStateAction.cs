using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Exceptions;
using RulesEngine.Actions;
using RulesEngine.Models;

namespace Flownodes.Worker.Workflows;

public class UpdateDeviceStateAction : ActionBase
{
    private readonly IResourceManagerGrain _resourceManager;

    public UpdateDeviceStateAction(IResourceManagerGrain resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public override async ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        var tenantName = context.GetContext<string>("tenantName");
        var deviceName = context.GetContext<string>("deviceName");
        var state = context.GetContext<Dictionary<string, object?>>("deviceState");

        var grain = await _resourceManager.GetResourceAsync<IDeviceGrain>(tenantName, deviceName);
        if (grain is null) throw new ResourceNotFoundException(tenantName, deviceName);
        await grain.UpdateStateAsync(state);
        return ValueTask.CompletedTask;
    }
}