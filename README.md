# FolderBackup

A small Windows tray app that backs up folders with **robocopy**, manually or on a schedule.
Robocopy only ever **adds or updates** files in the backup; it never deletes anything.

## Build and run

Requires Windows and the .NET 10 SDK.

```
dotnet build FolderBackup.slnx -c Release
src\FolderBackup.App\bin\Release\net10.0-windows\FolderBackup.exe
```

Right-click the tray icon for **Back up now** (all enabled jobs), **Settings…**, **Open log folder** and **Exit**.
Double-clicking the icon opens Settings, where **Run now** runs the selected job on its own, even if it's disabled.
Only one backup runs at a time. Starting another one while a backup is running just brings its progress window back.

## Where the files go

Each job copies its source folder *into* the backup folder, as a subfolder with the same name:

| Source | Backup folder | Files end up in |
|---|---|---|
| `C:\Users\you\Pictures` | `E:\Backup` | `E:\Backup\Pictures` |
| `C:\Users\you\Documents` | `E:\Backup` | `E:\Backup\Documents` |
| `D:\` (a drive root) | `E:\Backup` | `E:\Backup\D` |

So every job can share one backup folder. The job editor shows the final location as you type.
Two jobs can't back up into the same folder (or one inside the other); the editor blocks that and asks you to choose another backup folder.
This rule lives in one place, `IBackupTargetResolver`, which the command builder, validator and UI all use.

## Backup modes

| Mode | Robocopy flags | Behaviour |
|---|---|---|
| Append only | `/XC /XN /XO` | Copies new files only; existing backup files are never touched |
| Update if newer | `/XO` | Also replaces a backup file when the source is newer |
| Overwrite | *(none)* | Also replaces any backup file that differs |

Every run also uses `/E /COPY:DAT /DCOPY:T /XX /XJ /R:n /W:n` (plus `/FFT` by default).
`/XX` means files that exist only in the backup are ignored, never removed.
The command builder refuses `/MIR`, `/PURGE`, `/MOV` and `/MOVE` outright.

## How it works

Each job runs in two passes:

1. **Scan.** A dry run with `/L` lists what would be copied, which gives the progress bars real file and byte totals.
2. **Copy.** The real run. Robocopy's per-file percentages drive the "current file" bar, and bytes drive the "this job" bar.

Robocopy's output is localized ("New File" is "Neue Datei" on German Windows).
So `RobocopyOutputParser` recognizes lines by their *structure* (tabs, byte sizes, `(0x…)` error codes), not by their words.

**Scheduling** registers a Windows Task Scheduler task, *FolderBackup - Scheduled backup*, that runs `FolderBackup.exe --run`.
That means scheduled backups happen even when the tray app isn't open.
If the PC was off at the scheduled time, the task runs as soon as possible afterwards.
A cross-process lock stops a scheduled run and a manual run from copying at the same time.

## Where things live

| What | Where |
|---|---|
| Jobs and schedule | `%AppData%\FolderBackup\settings.json` |
| Run logs (one per run, kept 30 days) | `%AppData%\FolderBackup\Logs` |
| Tuning (retries, `/FFT`, robocopy path, data folder) | `appsettings.json` next to the exe |

## Project layout

```
src/
├── FolderBackup.Core/            UI-free logic
│   ├── Constants/                RobocopyFlags, AppConstants, ValidationMessages, CommandLineArguments
│   ├── Extensions/               DI registration, exit-code helpers
│   ├── Models/                   BackupJob, BackupSchedule, BackupProgress, results, parsed output lines
│   ├── Options/                  RobocopyOptions, StorageOptions
│   ├── Repositories/             JSON settings persistence
│   └── Services/                 Target resolver, command builder, output parser, process runner,
│                                 validator, run lock, log files, Task Scheduler, backup orchestration
└── FolderBackup.App/             WinForms tray app
    ├── Constants/                UiText, ProcessExitCodes
    ├── Forms/                    SettingsForm, JobEditorForm, ProgressForm (built in code, no designer files)
    ├── Presentation/             Display helpers and formatters
    ├── Services/                 Backup launcher (one progress window), form factory, headless runner
    └── Tray/                     TrayApplicationContext
```

Every service sits behind an interface and is injected through its constructor.
The parser, command builder, validator and progress tracker are pure logic and easy to unit test.

## Caveats

- **The code has not been compiled yet.** It was written without access to a .NET SDK. Expect to fix a small typo or two on the first build.
- **Locked files.** Robocopy can't copy files that another program has open (for example an open Outlook `.pst`). They're retried, reported as errors and logged. The next run picks them up.
- **No `/MT`.** Multithreaded copying turns off robocopy's per-file progress output, so it isn't used.
- **"Files copied"** counts files that robocopy reported as reaching 100%.
- **Scheduled runs happen only while you're logged on.** The task uses your normal user account and doesn't store a password.
- **If you move the exe,** open Settings and click Save again. This updates the scheduled task with the new path.
- **The icon** is Windows' default application icon. Add your own `.ico` file and point `NotifyIcon.Icon` at it.
- **The TaskScheduler NuGet package** is pinned at 2.11.0. Update it if a newer version is available.
- **Targeting .NET 8** only needs a change in `Directory.Build.props`, plus replacing `System.Threading.Lock` with `object` (it's used in two places).
