namespace Flownodes.Sdk.Resourcing.Behaviours;

public interface IBehaviour
{
    Task OnSetupAsync(ResourceContext context)
    {
        return Task.CompletedTask;
    }
}