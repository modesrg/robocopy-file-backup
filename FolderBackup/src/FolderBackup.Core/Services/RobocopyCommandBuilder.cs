using FolderBackup.Core.Constants;
using FolderBackup.Core.Models;
using FolderBackup.Core.Options;
using Microsoft.Extensions.Options;

namespace FolderBackup.Core.Services;

public sealed class RobocopyCommandBuilder : IRobocopyCommandBuilder
{
    private readonly IBackupTargetResolver _targetResolver;
    private readonly RobocopyOptions _options;

    public RobocopyCommandBuilder(IBackupTargetResolver targetResolver, IOptions<RobocopyOptions> options)
    {
        _targetResolver = targetResolver;
        _options = options.Value;
    }

    public IReadOnlyList<string> BuildListArguments(BackupJob job) => Build(job, listOnly: true);

    public IReadOnlyList<string> BuildCopyArguments(BackupJob job) => Build(job, listOnly: false);

    private List<string> Build(BackupJob job, bool listOnly)
    {
        ArgumentNullException.ThrowIfNull(job);

        List<string> arguments =
        [
            FormatPath(job.SourcePath),
            FormatPath(_targetResolver.GetTargetPath(job)),
            RobocopyFlags.IncludeSubdirectories,
            RobocopyFlags.CopyDataAttributesTimestamps,
            RobocopyFlags.CopyDirectoryTimestamps,
            RobocopyFlags.ExcludeExtra,
            RobocopyFlags.ExcludeJunctions,
            RobocopyFlags.RetryCount(_options.RetryCount),
            RobocopyFlags.RetryWaitSeconds(_options.RetryWaitSeconds),
            RobocopyFlags.NoDirectoryList,
            RobocopyFlags.FullPaths,
            RobocopyFlags.SizesInBytes,
            RobocopyFlags.NoJobHeader,
            RobocopyFlags.NoJobSummary,
            .. GetModeFlags(job.Mode),
        ];

        if (_options.UseFatFileTimes)
        {
            arguments.Add(RobocopyFlags.FatFileTimes);
        }

        if (listOnly)
        {
            arguments.Add(RobocopyFlags.ListOnly);
            arguments.Add(RobocopyFlags.NoProgress);
        }

        EnsureNonDestructive(arguments);
        return arguments;
    }

    private static string[] GetModeFlags(BackupMode mode) => mode switch
    {
        BackupMode.AppendOnly => [RobocopyFlags.ExcludeChanged, RobocopyFlags.ExcludeNewer, RobocopyFlags.ExcludeOlder],
        BackupMode.OverwriteIfNewer => [RobocopyFlags.ExcludeOlder],
        BackupMode.Overwrite => [],
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown backup mode."),
    };

    /// <summary>
    /// Robocopy treats a backslash before a closing quote as an escape, so "C:\" breaks the command line.
    /// Trailing separators are removed, and a bare drive root becomes "C:\." which means the same thing.
    /// </summary>
    private static string FormatPath(string path)
    {
        var trimmed = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        return Path.EndsInDirectorySeparator(trimmed) ? trimmed + "." : trimmed;
    }

    private static void EnsureNonDestructive(IEnumerable<string> arguments)
    {
        var destructive = arguments.FirstOrDefault(RobocopyFlags.Destructive.Contains);
        if (destructive is not null)
        {
            throw new InvalidOperationException($"Refusing to run robocopy with the destructive switch {destructive}.");
        }
    }
}
