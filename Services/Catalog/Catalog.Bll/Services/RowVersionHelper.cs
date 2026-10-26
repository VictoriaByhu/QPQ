namespace Catalog.Bll.Services;

internal static class RowVersionHelper
{
    public static byte[]? Decode(string? value) => string.IsNullOrEmpty(value) ? null : Convert.FromBase64String(value);
}
