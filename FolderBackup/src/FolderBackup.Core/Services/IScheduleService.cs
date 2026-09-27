using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public interface IScheduleService
{
    /// <summary>Creates, updates or removes the scheduled task to match <paramref name="schedule"/>.</summary>
    void Apply(BackupSchedule schedule);

    /// <summary>The next time the scheduled task will run, or null if it isn't scheduled.</summary>
    DateTime? GetNextRunTime();
}
