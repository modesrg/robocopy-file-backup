using FolderBackup.App.Services;
using FolderBackup.App.Tray;
using Microsoft.Extensions.DependencyInjection;

namespace FolderBackup.App.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolderBackupApp(this IServiceCollection services)
    {
        // Forms aren't registered: IFormFactory creates them on demand with their dependencies.
        services.AddSingleton<IFormFactory, FormFactory>();
        services.AddSingleton<IHeadlessBackupRunner, HeadlessBackupRunner>();
        services.AddSingleton<TrayApplicationContext>();

        return services;
    }
}
