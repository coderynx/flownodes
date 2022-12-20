namespace Flownodes.Sdk.Resourcing;

public interface IBehaviour
{
    Task OnSetupAsync(ResourceContext context);
}