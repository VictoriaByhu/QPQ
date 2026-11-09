using System.Diagnostics;

namespace Catalog.Bll;

/// <summary>Джерело власних spans сервісу; ім'я збігається з ApplicationName, тож ServiceDefaults підписується автоматично.</summary>
internal static class Telemetry
{
    public static readonly ActivitySource Source = new("Catalog.Api");
}
