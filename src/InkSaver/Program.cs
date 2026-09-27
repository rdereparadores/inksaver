namespace InkSaver;

internal static class Program
{
    /// <summary>Command-line flag used by the "start with Windows" registry entry.</summary>
    public const string AutostartArgument = "--autostart";

    private const string MutexName = @"Local\InkSaver-4f6f1d0c-9a55-4d1e-9c2b-7d0f3b6b8a21";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        var autostart = args.Contains(AutostartArgument, StringComparer.OrdinalIgnoreCase);

        ApplicationConfiguration.Initialize();

        if (!isFirstInstance)
        {
            if (!autostart)
            {
                MessageBox.Show(Strings.AlreadyRunning, Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return;
        }

        Application.Run(new TrayAppContext());
    }
}
