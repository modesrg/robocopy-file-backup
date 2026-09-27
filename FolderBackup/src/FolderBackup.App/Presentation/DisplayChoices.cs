using FolderBackup.App.Constants;
using FolderBackup.Core.Models;

namespace FolderBackup.App.Presentation;

internal static class DisplayChoices
{
    public static readonly IReadOnlyList<DisplayItem<BackupMode>> Modes =
    [
        new(BackupMode.AppendOnly, UiText.ModeAppendOnly),
        new(BackupMode.OverwriteIfNewer, UiText.ModeOverwriteIfNewer),
        new(BackupMode.Overwrite, UiText.ModeOverwrite),
    ];

    public static readonly IReadOnlyList<DisplayItem<ScheduleFrequency>> Frequencies =
    [
        new(ScheduleFrequency.Daily, UiText.FrequencyDaily),
        new(ScheduleFrequency.Weekly, UiText.FrequencyWeekly),
    ];

    public static DisplayItem<BackupMode> GetMode(BackupMode mode) => Modes.First(item => item.Value == mode);

    public static DisplayItem<ScheduleFrequency> GetFrequency(ScheduleFrequency frequency) =>
        Frequencies.First(item => item.Value == frequency);

    public static string GetModeDescription(BackupMode mode) => mode switch
    {
        BackupMode.AppendOnly => UiText.ModeAppendOnlyDescription,
        BackupMode.OverwriteIfNewer => UiText.ModeOverwriteIfNewerDescription,
        BackupMode.Overwrite => UiText.ModeOverwriteDescription,
        _ => string.Empty,
    };
}
