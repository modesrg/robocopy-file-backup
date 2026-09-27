namespace FolderBackup.Core.Constants;

public static class ValidationMessages
{
    public const string NameRequired = "Give the job a name.";
    public const string SourceRequired = "Choose a source folder.";
    public const string DestinationRequired = "Choose a backup folder.";
    public const string SameFolder = "The source and backup folders must be different.";
    public const string DestinationInsideSource = "The backup folder can't be inside the source folder, or the backup would copy itself.";
    public const string SourceInsideDestination = "The source folder can't be inside the backup folder.";

    public static string InvalidPath(string path) => $"\"{path}\" is not a valid path.";

    public static string PathMustBeAbsolute(string path) => $"\"{path}\" must be a full path, like C:\\Folder or \\\\server\\share.";

    public static string SourceMissing(string path) => $"The source folder \"{path}\" doesn't exist.";

    public static string DestinationDriveMissing(string root) => $"The backup drive \"{root}\" isn't available. Is it connected?";
}
