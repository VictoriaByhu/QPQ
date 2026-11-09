using System.Diagnostics;

namespace UserSkills.Bll;

/// <summary>Джерело власних spans сервісу; ім'я збігається з ApplicationName.</summary>
internal static class Telemetry
{
    public static readonly ActivitySource Source = new("UserSkills.Api");
}
