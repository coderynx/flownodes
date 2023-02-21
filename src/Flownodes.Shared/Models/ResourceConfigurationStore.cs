using System.Text.Json.Serialization;

namespace Flownodes.Shared.Models;

[GenerateSerializer]
public class ResourceConfigurationStore
{
    [Id(0)] public Dictionary<string, object?> Properties { get; set; } = new ();
    [Id(1)] public string? BehaviourId { get; set; }

    public object? this[string key]
    {
        get => Properties[key];
        set => Properties[key] = value;
    }

    [JsonIgnore] public int Count => Properties.Count;

    [JsonIgnore] public IEnumerable<string> Keys => Properties.Keys;

    [JsonIgnore] public IEnumerable<object?> Values => Properties.Values;

    public static ResourceConfigurationStore FromDictionary(Dictionary<string, object?> dictionary)
    {
        return new ResourceConfigurationStore
        {
            Properties = dictionary
        };
    }

    public void Add(string key, object? value)
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

    public bool TryGetValue(string key, out object? value)
    {
        return Properties.TryGetValue(key, out value);
    }
}