using System.Diagnostics.CodeAnalysis;
using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public sealed class BackupTargetResolver : IBackupTargetResolver
{
    public string GetTargetPath(BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return TryGetTargetPath(job, out var targetPath)
            ? targetPath
            : throw new ArgumentException("The job's source and backup folders must be valid full paths.", nameof(job));
    }

    public bool TryGetTargetPath(BackupJob job, [NotNullWhen(true)] out string? targetPath)
    {
        ArgumentNullException.ThrowIfNull(job);
        targetPath = null;

        if (!IsFullyQualified(job.SourcePath) || !IsFullyQualified(job.DestinationPath))
        {
            return false;
        }

        try
        {
            var source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(job.SourcePath));
            targetPath = Path.Combine(Path.GetFullPath(job.DestinationPath), GetFolderName(source));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static bool IsFullyQualified(string path) =>
        !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path);

    /// <summary>The source's folder name. A drive root like C:\ has none, so its drive letter is used instead.</summary>
    private static string GetFolderName(string sourcePath)
    {
        var name = Path.GetFileName(sourcePath);
        return string.IsNullOrEmpty(name)
            ? sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar)
            : name;
    }
}
