using System.Text.Json;

namespace InkSaver.Core;

/// <summary>Loads and saves <see cref="AppSettings"/> as a JSON file.</summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsStore(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    /// <summary>Default location: <c>%APPDATA%\InkSaver\settings.json</c>.</summary>
    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InkSaver");

    public static SettingsStore CreateDefault() => new(Path.Combine(DefaultDirectory, "settings.json"));

    /// <summary>Returns the stored settings, or fresh defaults when the file is missing or unreadable.</summary>
    public AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // A corrupt or locked file must never keep the app from starting.
        }

        return new AppSettings();
    }

    /// <summary>Writes the settings atomically so a crash never leaves a half-written file.</summary>
    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(tempPath, FilePath, overwrite: true);
    }
}
