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

    public static bool HasChanged<TKey, TValue>(this Dictionary<TKey, TValue?> oldState,
        Dictionary<TKey, TValue?> newState) where TKey : notnull
    {
        return oldState.Any(pair => !newState.Contains(pair));
    }

    public static bool ContainsAll<TKey, TValue>(this Dictionary<TKey, TValue> mainDict,
        Dictionary<TKey, TValue> subDict) where TKey : notnull
    {
        foreach (var (subKey, subValue) in subDict)
            if (!mainDict.TryGetValue(subKey, out var mainValue) || !AreValuesEqual<TKey, TValue>(subValue, mainValue))
                return false;

        return true;
    }

    private static bool AreValuesEqual<TKey, TValue>(TValue subValue, TValue mainValue) where TKey : notnull
    {
        if (subValue is null) return mainValue is null;

        if (subValue is Dictionary<TKey, TValue> subNestedDict && mainValue is Dictionary<TKey, TValue> mainNestedDict)
            return mainNestedDict.ContainsAll(subNestedDict);

        return subValue.Equals(mainValue);
    }
}