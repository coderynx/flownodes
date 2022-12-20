namespace Flownodes.Sdk.Resourcing;

public class ActualResourceConfiguration
{
    public Dictionary<string, object?> Properties { get; init; } = new();

    public string? BehaviourId { get; set; }

    public object? this[string key]
    {
        get => Properties[key];
        set => Properties[key] = value;
    }

    public int Count => Properties.Count;

    public IEnumerable<string> Keys => Properties.Keys;

    public IEnumerable<object?> Values => Properties.Values;

    public static ActualResourceConfiguration FromDictionary(Dictionary<string, object?> dictionary)
    {
        return new ActualResourceConfiguration
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