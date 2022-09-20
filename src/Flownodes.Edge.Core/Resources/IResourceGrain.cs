namespace Flownodes.Edge.Core.Resources;

public interface IResourceGrain
{
    Task ConfigureAsync(Dictionary<string, string> configuration);
    Task<ResourceIdentityCard> GetIdentityCard();
    Task SelfRemoveAsync();
}