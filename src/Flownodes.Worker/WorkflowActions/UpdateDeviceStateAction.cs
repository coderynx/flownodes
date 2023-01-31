using Flownodes.Shared.Interfaces;
using RulesEngine.Actions;
using RulesEngine.Models;

namespace Flownodes.Worker.WorkflowActions;

public class UpdateDeviceStateAction : ActionBase
{
    private readonly IResourceManagerGrain _resourceManager;

    public UpdateDeviceStateAction(IResourceManagerGrain resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public override async ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        var deviceId = context.GetContext<string>("deviceId");
        var state = context.GetContext<Dictionary<string, object?>>("deviceState");

        var grain = await _resourceManager.GetResourceAsync<IDeviceGrain>(deviceId);
        await grain.UpdateStateAsync(state);
        return ValueTask.CompletedTask;
    }
}