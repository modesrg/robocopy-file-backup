using System.Collections.Frozen;

namespace FolderBackup.Core.Constants;

/// <summary>
/// Robocopy switches used by the app.
/// Reference: https://learn.microsoft.com/windows-server/administration/windows-commands/robocopy
/// </summary>
public static class RobocopyFlags
{
    // What to copy
    public const string IncludeSubdirectories = "/E";
    public const string CopyDataAttributesTimestamps = "/COPY:DAT";
    public const string CopyDirectoryTimestamps = "/DCOPY:T";

    // Which source files to skip. The combinations of these define the backup modes.
    public const string ExcludeChanged = "/XC";
    public const string ExcludeNewer = "/XN";
    public const string ExcludeOlder = "/XO";

    // Safety: never consider files that exist only in the destination, and don't follow junctions.
    public const string ExcludeExtra = "/XX";
    public const string ExcludeJunctions = "/XJ";

    // Reliability
    public const string FatFileTimes = "/FFT";
    public const string RetryCountPrefix = "/R:";
    public const string RetryWaitPrefix = "/W:";

    // Output shaping, so the output can be parsed reliably
    public const string ListOnly = "/L";
    public const string NoProgress = "/NP";
    public const string NoDirectoryList = "/NDL";
    public const string FullPaths = "/FP";
    public const string SizesInBytes = "/BYTES";
    public const string NoJobHeader = "/NJH";
    public const string NoJobSummary = "/NJS";

    /// <summary>Switches that delete or move files. The app never uses them, and the command builder refuses them.</summary>
    public static readonly FrozenSet<string> Destructive =
        new[] { "/MIR", "/PURGE", "/MOV", "/MOVE" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static string RetryCount(int count) => $"{RetryCountPrefix}{count}";

    public static string RetryWaitSeconds(int seconds) => $"{RetryWaitPrefix}{seconds}";
}
