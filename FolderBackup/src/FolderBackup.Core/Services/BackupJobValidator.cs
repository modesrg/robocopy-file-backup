using FolderBackup.Core.Constants;
using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public sealed class BackupJobValidator : IBackupJobValidator
{
    public ValidationResult ValidateConfiguration(BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(job.Name))
        {
            errors.Add(ValidationMessages.NameRequired);
        }

        var source = TryGetFullPath(job.SourcePath, ValidationMessages.SourceRequired, errors);
        var destination = TryGetFullPath(job.DestinationPath, ValidationMessages.DestinationRequired, errors);

        if (source is not null && destination is not null)
        {
            AddOverlapErrors(source, destination, errors);
        }

        return new ValidationResult(errors);
    }

    public ValidationResult ValidateReadyToRun(BackupJob job)
    {
        var configuration = ValidateConfiguration(job);
        if (!configuration.IsValid)
        {
            return configuration;
        }

        var errors = new List<string>();

        if (!Directory.Exists(job.SourcePath))
        {
            errors.Add(ValidationMessages.SourceMissing(job.SourcePath));
        }

        var destinationRoot = Path.GetPathRoot(Path.GetFullPath(job.DestinationPath));
        if (string.IsNullOrEmpty(destinationRoot) || !Directory.Exists(destinationRoot))
        {
            errors.Add(ValidationMessages.DestinationDriveMissing(destinationRoot ?? job.DestinationPath));
        }

        return new ValidationResult(errors);
    }

    private static string? TryGetFullPath(string path, string requiredMessage, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add(requiredMessage);
            return null;
        }

        // Relative paths are dangerous here: the scheduled task runs with a different working directory.
        if (!Path.IsPathFullyQualified(path))
        {
            errors.Add(ValidationMessages.PathMustBeAbsolute(path));
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add(ValidationMessages.InvalidPath(path));
            return null;
        }
    }

    private static void AddOverlapErrors(string source, string destination, List<string> errors)
    {
        var sourceDirectory = WithTrailingSeparator(source);
        var destinationDirectory = WithTrailingSeparator(destination);

        if (string.Equals(sourceDirectory, destinationDirectory, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(ValidationMessages.SameFolder);
        }
        else if (destinationDirectory.StartsWith(sourceDirectory, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(ValidationMessages.DestinationInsideSource);
        }
        else if (sourceDirectory.StartsWith(destinationDirectory, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(ValidationMessages.SourceInsideDestination);
        }
    }

    private static string WithTrailingSeparator(string path) =>
        Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;
}
