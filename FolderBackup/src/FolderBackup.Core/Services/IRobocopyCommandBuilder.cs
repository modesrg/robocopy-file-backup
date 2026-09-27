using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public interface IRobocopyCommandBuilder
{
    /// <summary>Arguments for a dry run (/L) that lists what would be copied, without copying.</summary>
    IReadOnlyList<string> BuildListArguments(BackupJob job);

    /// <summary>Arguments for the real copy.</summary>
    IReadOnlyList<string> BuildCopyArguments(BackupJob job);
}
