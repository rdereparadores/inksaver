using Microsoft.Win32;

namespace InkSaver;

/// <summary>Manages the per-user "start with Windows" entry (no admin rights needed).</summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    // Where Task Manager / Settings > Apps > Startup store whether a Run entry is disabled.
    private const string ApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    private const string ValueName = "InkSaver";

    private static string Command => $"\"{Environment.ProcessPath}\" {Program.AutostartArgument}";

    public static bool IsEnabled
    {
        get
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            if (runKey?.GetValue(ValueName) is not string)
            {
                return false;
            }

            // First byte 0x02 = enabled, 0x03 = disabled by the user in Task Manager.
            using var approvedKey = Registry.CurrentUser.OpenSubKey(ApprovedKeyPath);
            return approvedKey?.GetValue(ValueName) is not byte[] { Length: > 0 } state || (state[0] & 1) == 0;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using (var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath))
        {
            if (enabled)
            {
                runKey.SetValue(ValueName, Command);
            }
            else
            {
                runKey.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }

        // Clear any "disabled" flag left by Task Manager so our checkbox is the source of truth.
        using var approvedKey = Registry.CurrentUser.OpenSubKey(ApprovedKeyPath, writable: true);
        approvedKey?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>Keeps the entry pointing at the current executable if the user moved it.</summary>
    public static void RefreshPathIfEnabled()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        if (runKey?.GetValue(ValueName) is string current && !string.Equals(current, Command, StringComparison.OrdinalIgnoreCase))
        {
            using var writable = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            writable.SetValue(ValueName, Command);
        }
    }
}
