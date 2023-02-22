using System.Text.Json.Serialization;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
internal sealed class ResourceStateStore
{
    [Id(1)] private Dictionary<string, object?> _properties = new();
    [Id(0)] public DateTime? LastUpdate { get; set; } = DateTime.Now;

    public Dictionary<string, object?> Properties
    {
        get => _properties;
        set
        {
            _properties = value;
            LastUpdate = DateTime.Now;
        }
    }

    public object? this[string key]
    {
        get => Properties[key];
        set
        {
            Properties[key] = value;
            LastUpdate = DateTime.Now;
        }
    }

    [JsonIgnore] public int Count => Properties.Count;

    [JsonIgnore] public IEnumerable<string> Keys => Properties.Keys;

    [JsonIgnore] public IEnumerable<object?> Values => Properties.Values;

    public void Add(string key, object? value)
    {
        Properties.Add(key, value);
        LastUpdate = DateTime.Now;
    }

    public bool ContainsKey(string key)
    {
        return Properties.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        var result = Properties.Remove(key);
        if (!result) return false;

        LastUpdate = DateTime.Now;
        return true;
    }

    public bool TryGetValue(string key, out object? value)
    {
        return Properties.TryGetValue(key, out value);
    }

    public object? GetValue(string key)
    {
        return Properties.GetValueOrDefault(key);
    }
}