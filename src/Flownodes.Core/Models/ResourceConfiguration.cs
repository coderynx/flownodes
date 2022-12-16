namespace Flownodes.Core.Models;

[GenerateSerializer]
public class ResourceConfiguration
{
    [Id(0)] public Dictionary<string, object?> Dictionary { get; init; } = new();

    public object? this[string key]
    {
        get => Dictionary[key];
        set => Dictionary[key] = value;
    }

    public int Count => Dictionary.Count;

    public IEnumerable<string> Keys => Dictionary.Keys;

    public IEnumerable<object?> Values => Dictionary.Values;

    public void Add(string key, object? value)
    {
        Dictionary.Add(key, value);
    }

    public bool ContainsKey(string key)
    {
        return Dictionary.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        return Dictionary.Remove(key);
    }

    public bool TryGetValue(string key, out object? value)
    {
        return Dictionary.TryGetValue(key, out value);
    }
}