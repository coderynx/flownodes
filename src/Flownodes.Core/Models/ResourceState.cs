namespace Flownodes.Core.Models;

[GenerateSerializer]
public class ResourceState
{
    [Id(0)] public DateTime? LastUpdate { get; private set; }
    [Id(1)] public Dictionary<string, object?> Dictionary { get; init; } = new();

    public object? this[string key]
    {
        get => Dictionary[key];
        set
        {
            Dictionary[key] = value;
            LastUpdate = DateTime.Now;
        }
    }

    public int Count => Dictionary.Count;

    public IEnumerable<string> Keys => Dictionary.Keys;

    public IEnumerable<object?> Values => Dictionary.Values;

    public void Add(string key, object? value)
    {
        Dictionary.Add(key, value);
        LastUpdate = DateTime.Now;
    }

    public bool ContainsKey(string key)
    {
        return Dictionary.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        var result = Dictionary.Remove(key);
        if (!result) return false;

        LastUpdate = DateTime.Now;
        return true;
    }

    public bool TryGetValue(string key, out object? value)
    {
        return Dictionary.TryGetValue(key, out value);
    }

    public object? GetValue(string key)
    {
        return Dictionary.GetValueOrDefault(key);
    }
}