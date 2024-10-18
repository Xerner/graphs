namespace Graphs.Extensions;

public static class DictionaryExtensions
{
    public static bool TryGetValueWithGenerics<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, out TValue? value) 
        where TKey : notnull
    {
        if (dict.TryGetValue(key, out value))
        {
            return true;
        }
        foreach (var key_ in dict.Keys)
        {
            if (key.GetType().IsAssignableFrom(key_.GetType()))
            {
                value = dict[key_];
                return true;
            }
        }
        value = default;
        return false;
    }
}
