namespace Flownodes.Worker.Extensions;

public static class DictionaryExtensions
{
    public static void MergeInPlace<TKey, TValue>(this IDictionary<TKey, TValue?> left,
        IDictionary<TKey, TValue?> right) where TKey : notnull
    {
        foreach (var (key, value) in right)
            if (!left.ContainsKey(key)) left.Add(key, value);
            else left[key] = value;
    }
}