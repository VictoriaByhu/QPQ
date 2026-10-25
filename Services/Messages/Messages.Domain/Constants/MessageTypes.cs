namespace Messages.Domain.Constants;

public static class MessageTypes
{
    public const string Text = "text";
    public const string File = "file";
    public const string System = "system";

    public static readonly IReadOnlyList<string> All = [Text, File, System];
}
