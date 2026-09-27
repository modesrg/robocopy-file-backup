using FolderBackup.Core.Constants;
using FolderBackup.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.TaskScheduler;

namespace FolderBackup.Core.Services;

/// <summary>
/// Registers the backup in Windows Task Scheduler, so it runs on time even when the tray app is closed.
/// The task runs this same executable with <see cref="CommandLineArguments.RunBackup"/>.
/// </summary>
public sealed class WindowsTaskSchedulerService : IScheduleService
{
    private readonly ILogger<WindowsTaskSchedulerService> _logger;

    public WindowsTaskSchedulerService(ILogger<WindowsTaskSchedulerService> logger)
    {
        _logger = logger;
    }

    public void Apply(BackupSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        using var taskService = new TaskService();

        if (!schedule.IsEnabled)
        {
            taskService.RootFolder.DeleteTask(AppConstants.ScheduledTaskName, exceptionOnNotExists: false);
            _logger.LogInformation("Scheduled backup removed.");
            return;
        }

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not determine the path of the running executable.");

        var definition = taskService.NewTask();
        definition.RegistrationInfo.Description = AppConstants.ScheduledTaskDescription;
        definition.Triggers.Add(CreateTrigger(schedule));
        definition.Actions.Add(new ExecAction(executablePath, CommandLineArguments.RunBackup, AppContext.BaseDirectory));

        // Catch up after the PC was off or asleep at the scheduled time.
        definition.Settings.StartWhenAvailable = true;
        definition.Settings.DisallowStartIfOnBatteries = false;
        definition.Settings.StopIfGoingOnBatteries = false;
        definition.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;

        using var registered = taskService.RootFolder.RegisterTaskDefinition(AppConstants.ScheduledTaskName, definition);
        _logger.LogInformation("Scheduled backup registered: {Frequency} at {Time}.", schedule.Frequency, schedule.TimeOfDay);
    }

    public DateTime? GetNextRunTime()
    {
        using var taskService = new TaskService();
        using var task = taskService.GetTask(AppConstants.ScheduledTaskName);

        return task is { Enabled: true } && task.NextRunTime != DateTime.MinValue
            ? task.NextRunTime
            : null;
    }

    private static Trigger CreateTrigger(BackupSchedule schedule)
    {
        var startBoundary = DateTime.Today.Add(schedule.TimeOfDay.ToTimeSpan());

        return schedule.Frequency switch
        {
            ScheduleFrequency.Daily => new DailyTrigger { StartBoundary = startBoundary },
            ScheduleFrequency.Weekly => new WeeklyTrigger(ToDaysOfTheWeek(schedule.DaysOfWeek)) { StartBoundary = startBoundary },
            _ => throw new ArgumentOutOfRangeException(nameof(schedule), schedule.Frequency, "Unknown schedule frequency."),
        };
    }

    private static DaysOfTheWeek ToDaysOfTheWeek(IReadOnlyList<DayOfWeek> days)
    {
        if (days.Count == 0)
        {
            throw new ArgumentException("A weekly schedule needs at least one day.", nameof(days));
        }

        // DayOfWeek is 0 (Sunday) to 6 (Saturday); DaysOfTheWeek is a bit field starting at Sunday = 1.
        return days.Aggregate((DaysOfTheWeek)0, (all, day) => all | (DaysOfTheWeek)(1 << (int)day));
    }
}
