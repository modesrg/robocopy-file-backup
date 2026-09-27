using FolderBackup.App.Constants;
using FolderBackup.App.Extensions;
using FolderBackup.App.Services;
using FolderBackup.App.Tray;
using FolderBackup.Core.Constants;
using FolderBackup.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FolderBackup.App;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        using var host = CreateHost();

        var isScheduledRun = args.Contains(CommandLineArguments.RunBackup, StringComparer.OrdinalIgnoreCase);
        return isScheduledRun
            ? RunScheduledBackup(host.Services)
            : RunTrayApplication(host.Services);
    }

    private static IHost CreateHost()
    {
        // Content root = the exe's folder, so appsettings.json is found even when
        // Task Scheduler starts the app with a different working directory.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Services
            .AddFolderBackupCore(builder.Configuration)
            .AddFolderBackupApp();

        return builder.Build();
    }

    private static int RunScheduledBackup(IServiceProvider services)
    {
        // Main can't be both async and [STAThread]. There's no UI or synchronization context
        // in a scheduled run, so blocking here is safe.
        var runner = services.GetRequiredService<IHeadlessBackupRunner>();
        return runner.RunAsync().GetAwaiter().GetResult();
    }

    private static int RunTrayApplication(IServiceProvider services)
    {
        ApplicationConfiguration.Initialize();

        using var instanceMutex = new Mutex(initiallyOwned: true, AppConstants.TrayInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(UiText.AlreadyRunning, UiText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return ProcessExitCodes.Success;
        }

        Application.Run(services.GetRequiredService<TrayApplicationContext>());
        return ProcessExitCodes.Success;
    }
}
