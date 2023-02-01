using Flownodes.Shared.Interfaces;
using RulesEngine.Actions;
using RulesEngine.Models;

namespace Flownodes.Worker.WorkflowActions;

public class GetDeviceStateAction : ActionBase
{
    private readonly IResourceManagerGrain _resourceManager;

    public GetDeviceStateAction(IResourceManagerGrain resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public override async ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        var deviceId = context.GetContext<string>("deviceId");

        var grain = await _resourceManager.GetResourceAsync<IDeviceGrain>(deviceId);
        return await grain.GetState();
    }
}