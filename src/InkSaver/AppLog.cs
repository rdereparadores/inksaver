using InkSaver.Core;

namespace InkSaver;

/// <summary>Small append-only history of what InkSaver did, kept next to the settings.</summary>
internal static class AppLog
{
    private const long MaxSizeBytes = 256 * 1024;

    public static string FilePath { get; } = Path.Combine(SettingsStore.DefaultDirectory, "inksaver.log");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(SettingsStore.DefaultDirectory);
            TrimIfTooLarge();
            File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging is best effort only.
        }
    }

    private static void TrimIfTooLarge()
    {
        var info = new FileInfo(FilePath);
        if (!info.Exists || info.Length <= MaxSizeBytes)
        {
            return;
        }

        var lines = File.ReadAllLines(FilePath);
        File.WriteAllLines(FilePath, lines.Skip(lines.Length / 2));
    }
}
