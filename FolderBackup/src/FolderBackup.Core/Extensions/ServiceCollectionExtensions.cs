using FolderBackup.Core.Options;
using FolderBackup.Core.Repositories;
using FolderBackup.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FolderBackup.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolderBackupCore(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RobocopyOptions>(configuration.GetSection(RobocopyOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        // Everything is stateless, so singletons are fine for a desktop app.
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IRobocopyOutputParser, RobocopyOutputParser>();
        services.AddSingleton<IRobocopyCommandBuilder, RobocopyCommandBuilder>();
        services.AddSingleton<IRobocopyRunner, RobocopyRunner>();
        services.AddSingleton<IBackupTargetResolver, BackupTargetResolver>();
        services.AddSingleton<IBackupJobValidator, BackupJobValidator>();
        services.AddSingleton<IBackupRunLock, NamedSemaphoreRunLock>();
        services.AddSingleton<IBackupLogFactory, FileBackupLogFactory>();
        services.AddSingleton<IScheduleService, WindowsTaskSchedulerService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IConfiguredBackupService, ConfiguredBackupService>();

        return services;
    }
}
