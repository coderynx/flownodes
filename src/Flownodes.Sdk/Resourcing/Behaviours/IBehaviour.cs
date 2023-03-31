namespace Flownodes.Sdk.Resourcing.Behaviours;

public interface IBehaviour
{
    Task<UpdateResourceBag> OnSetupAsync()
    {
        return Task.FromResult(new UpdateResourceBag());
    }
}