using FolderBackup.Core.Constants;
using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public sealed class BackupJobValidator : IBackupJobValidator
{
    private readonly IBackupTargetResolver _targetResolver;

    public BackupJobValidator(IBackupTargetResolver targetResolver)
    {
        _targetResolver = targetResolver;
    }

    public ValidationResult ValidateConfiguration(BackupJob job, IReadOnlyCollection<BackupJob> otherJobs)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(otherJobs);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(job.Name))
        {
            errors.Add(ValidationMessages.NameRequired);
        }

        var sourceIsValid = ValidatePath(job.SourcePath, ValidationMessages.SourceRequired, errors);
        var destinationIsValid = ValidatePath(job.DestinationPath, ValidationMessages.DestinationRequired, errors);

        if (sourceIsValid && destinationIsValid && _targetResolver.TryGetTargetPath(job, out var target))
        {
            AddOverlapErrors(Path.GetFullPath(job.SourcePath), target, errors);
            AddConflictErrors(job, target, otherJobs, errors);
        }

        return new ValidationResult(errors);
    }

    public ValidationResult ValidateReadyToRun(BackupJob job)
    {
        var configuration = ValidateConfiguration(job, []);
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

    private static bool ValidatePath(string path, string requiredMessage, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add(requiredMessage);
            return false;
        }

        // Relative paths are dangerous here: the scheduled task runs with a different working directory.
        if (!Path.IsPathFullyQualified(path))
        {
            errors.Add(ValidationMessages.PathMustBeAbsolute(path));
            return false;
        }

        try
        {
            Path.GetFullPath(path);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add(ValidationMessages.InvalidPath(path));
            return false;
        }
    }

    private static void AddOverlapErrors(string source, string target, List<string> errors)
    {
        if (IsSameOrInside(target, source))
        {
            errors.Add(IsSameOrInside(source, target)
                ? ValidationMessages.SourceIsTarget(target)
                : ValidationMessages.TargetInsideSource(target));
        }
        else if (IsSameOrInside(source, target))
        {
            errors.Add(ValidationMessages.SourceInsideTarget(target));
        }
    }

    /// <summary>Two jobs must never write into the same folder, or into each other's folders.</summary>
    private void AddConflictErrors(BackupJob job, string target, IEnumerable<BackupJob> otherJobs, List<string> errors)
    {
        foreach (var other in otherJobs)
        {
            if (other.Id == job.Id || !_targetResolver.TryGetTargetPath(other, out var otherTarget))
            {
                continue;
            }

            if (IsSameOrInside(target, otherTarget) || IsSameOrInside(otherTarget, target))
            {
                errors.Add(ValidationMessages.TargetInUse(other.Name, otherTarget));
                return;
            }
        }
    }

    private static bool IsSameOrInside(string path, string folder) =>
        WithTrailingSeparator(path).StartsWith(WithTrailingSeparator(folder), StringComparison.OrdinalIgnoreCase);

    private static string WithTrailingSeparator(string path) =>
        Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;
}
