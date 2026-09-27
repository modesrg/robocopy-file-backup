using System.Text.Json;
using System.Text.Json.Serialization;
using FolderBackup.Core.Constants;
using FolderBackup.Core.Models;
using FolderBackup.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FolderBackup.Core.Repositories;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly StorageOptions _options;
    private readonly ILogger<JsonSettingsRepository> _logger;

    public JsonSettingsRepository(IOptions<StorageOptions> options, ILogger<JsonSettingsRepository> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BackupSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_options.SettingsFilePath))
        {
            _logger.LogInformation("No settings file at {Path}; starting with defaults.", _options.SettingsFilePath);
            return new BackupSettings();
        }

        await using var stream = File.OpenRead(_options.SettingsFilePath);
        var settings = await JsonSerializer.DeserializeAsync<BackupSettings>(stream, SerializerOptions, ct).ConfigureAwait(false);
        return settings ?? new BackupSettings();
    }

    public async Task SaveAsync(BackupSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(_options.DataDirectory);

        // Write to a temp file first, then swap it in, so a crash mid-save never leaves a half-written file.
        var tempPath = _options.SettingsFilePath + AppConstants.TempFileExtension;
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, ct).ConfigureAwait(false);
        }

        File.Move(tempPath, _options.SettingsFilePath, overwrite: true);
        _logger.LogInformation("Saved settings with {JobCount} job(s).", settings.Jobs.Count);
    }
}
