namespace FolderBackup.Core.Models;

public sealed record BackupSchedule
{
    public bool IsEnabled { get; init; }
    public ScheduleFrequency Frequency { get; init; } = ScheduleFrequency.Daily;
    public TimeOnly TimeOfDay { get; init; } = new(20, 0);
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; init; } = [DayOfWeek.Sunday];
}
