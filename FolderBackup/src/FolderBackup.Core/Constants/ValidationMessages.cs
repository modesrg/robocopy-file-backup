namespace FolderBackup.Core.Constants;

public static class ValidationMessages
{
    public const string NameRequired = "Give the job a name.";
    public const string SourceRequired = "Choose a source folder.";
    public const string DestinationRequired = "Choose a backup folder.";

    public static string InvalidPath(string path) => $"\"{path}\" is not a valid path.";

    public static string PathMustBeAbsolute(string path) => $"\"{path}\" must be a full path, like C:\\Folder or \\\\server\\share.";

    public static string SourceIsTarget(string target) =>
        $"The backup would go to \"{target}\", which is the source folder itself. Choose another backup folder.";

    public static string TargetInsideSource(string target) =>
        $"The backup would go to \"{target}\", inside the source folder, so it would copy itself. Choose another backup folder.";

    public static string SourceInsideTarget(string target) =>
        $"The source folder is inside the backup location \"{target}\". Choose another backup folder.";

    public static string TargetInUse(string otherJobName, string otherTarget) =>
        $"The job \"{otherJobName}\" already backs up to \"{otherTarget}\". Choose another backup folder for this job.";

    public static string SourceMissing(string path) => $"The source folder \"{path}\" doesn't exist.";

    public static string DestinationDriveMissing(string root) => $"The backup drive \"{root}\" isn't available. Is it connected?";
}
