namespace Flownodes.Shared.Resourcing.Grains;

public interface IJournaledStoreGrain<TState> : IGrainWithStringKey where TState : class, new()
{
    Task UpdateAsync(TState state);
    ValueTask<TState> Get();
    ValueTask<DateTime?> GetUpdatedAt();
    Task ClearAsync();
}