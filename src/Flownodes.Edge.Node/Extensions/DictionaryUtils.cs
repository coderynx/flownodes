namespace Flownodes.Edge.Node.Extensions;

public static class DictionaryUtils
{
    public static void MergeInPlace<TKey, TValue>(this Dictionary<TKey, TValue?> left,
        Dictionary<TKey, TValue?> right) where TKey : notnull
    {
        foreach (var (key, value) in right)
            if (!left.ContainsKey(key)) left.Add(key, value);
            else left[key] = value;
    }
}