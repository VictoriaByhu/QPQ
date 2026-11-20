using System.Text.Json;

namespace Platform.EventBus;

/// <summary>Wire format of event bodies: camelCase JSON, shared by publishers and consumers.</summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
