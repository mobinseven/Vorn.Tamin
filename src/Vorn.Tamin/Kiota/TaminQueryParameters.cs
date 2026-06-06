namespace Vorn.Tamin.Kiota;

/// <summary>Builds validated query parameter dictionaries for friendly domain clients.</summary>
internal static class TaminQueryParameters
{
    public static IReadOnlyDictionary<string, string?> Build(params (string key, string? value)[] pairs)
    {
        var query = new Dictionary<string, string?>(pairs.Length);
        foreach (var (key, value) in pairs)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Query keys must be provided.", nameof(pairs));

            if (!string.IsNullOrWhiteSpace(value))
                query[key] = value;
        }

        return query;
    }
}
