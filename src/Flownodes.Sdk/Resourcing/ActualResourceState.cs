namespace Flownodes.Sdk.Resourcing;

public class ActualResourceState
{
    public DateTime? LastUpdate { get; private set; }
    public Dictionary<string, object?> Properties { get; init; } = new();

    public object? this[string key]
    {
        get => Properties[key];
        set
        {
            Properties[key] = value;
            LastUpdate = DateTime.Now;
        }
    }

    public int Count => Properties.Count;

    public IEnumerable<string> Keys => Properties.Keys;

    public IEnumerable<object?> Values => Properties.Values;

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