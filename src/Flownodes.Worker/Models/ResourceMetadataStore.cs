using Newtonsoft.Json;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
internal sealed class ResourceMetadataStore
{
    [Id(0)] public Dictionary<string, string?> Properties { get; set; } = new();

    [Id(1)] public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? this[string key]
    {
        get => Properties[key];
        set => Properties[key] = value;
    }

    [JsonIgnore] public int Count => Properties.Count;

    [JsonIgnore] public IEnumerable<string> Keys => Properties.Keys;

    [JsonIgnore] public IEnumerable<string?> Values => Properties.Values;

    public static ResourceMetadataStore FromDictionary(Dictionary<string, string?> dictionary)
    {
        return new ResourceMetadataStore
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