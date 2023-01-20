namespace Flownodes.Sdk.Resourcing;

public class ResourceMetadata
{
    public Dictionary<string, string?> Properties { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public string? this[string key]
    {
        get => Properties[key];
        set => Properties[key] = value;
    }

    public int Count => Properties.Count;

    public IEnumerable<string> Keys => Properties.Keys;

    public IEnumerable<string?> Values => Properties.Values;

    public static ResourceMetadata FromDictionary(Dictionary<string, string?> dictionary)
    {
        return new ResourceMetadata
        {
            Properties = dictionary
        };
    }

    public void Add(string key, string? value)
    {
        Properties.Add(key, value);
    }

    public bool ContainsKey(string key)
    {
        return Properties.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        return Properties.Remove(key);
    }

    public bool TryGetValue(string key, out string? value)
    {
        return Properties.TryGetValue(key, out value);
    }
}