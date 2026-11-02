using FolderBackup.App.Constants;

namespace FolderBackup.App.Presentation;

/// <summary>
/// The app's icons, loaded once from embedded resources. Each icon holds every size (16–256 px),
/// so windows pick the right one automatically. Use <see cref="ForTray"/> for the notification area.
/// </summary>
internal static class AppIcons
{
    public static Icon Application { get; } = Load(ResourceNames.AppIcon);

    /// <summary>The app icon with an amber dot, shown while a backup is running.</summary>
    public static Icon Busy { get; } = Load(ResourceNames.BusyIcon);

    /// <summary>A copy at the system's small-icon size (16 px at 100% scaling, larger on high-DPI screens). The caller disposes it.</summary>
    public static Icon ForTray(Icon icon) => new(icon, SystemInformation.SmallIconSize);

    private static Icon Load(string resourceName)
    {
        using var stream = typeof(AppIcons).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The embedded icon '{resourceName}' is missing.");
        return new Icon(stream);
    }
}
