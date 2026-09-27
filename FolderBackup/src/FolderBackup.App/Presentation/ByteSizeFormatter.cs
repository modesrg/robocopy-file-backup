namespace FolderBackup.App.Presentation;

internal static class ByteSizeFormatter
{
    private const double Step = 1024;
    private static readonly string[] Units = ["bytes", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        double size = bytes;
        var unit = 0;

        while (size >= Step && unit < Units.Length - 1)
        {
            size /= Step;
            unit++;
        }

        return unit == 0 ? $"{bytes:N0} {Units[0]}" : $"{size:0.#} {Units[unit]}";
    }
}
