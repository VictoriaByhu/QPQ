using System.Text;

namespace Platform.EventBus;

public static class MessageHeaders
{
    public const string CorrelationId = "x-correlation-id";
    public const string SchemaVersion = "x-schema-version";
    public const string RetryCount = "x-retry-count";

    /// <summary>
    /// Reads a header as text. Strings sent by a client arrive as UTF-8 byte arrays,
    /// numbers set in the management UI may arrive as int or long.
    /// </summary>
    public static string? GetString(IDictionary<string, object?>? headers, string name)
    {
        if (headers is null || !headers.TryGetValue(name, out var value))
            return null;

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            null => null,
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public static int GetInt(IDictionary<string, object?>? headers, string name, int fallback)
        => int.TryParse(GetString(headers, name), out var value) ? value : fallback;
}
