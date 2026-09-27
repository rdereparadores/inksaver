using System.Diagnostics;
using System.Drawing.Printing;
using System.Reflection;
using InkSaver.Core;
using Microsoft.Win32;

namespace InkSaver;

/// <summary>
/// The whole application: a notification-area icon whose menu holds every setting,
/// plus a timer that sends the maintenance page when it is due.
/// </summary>
internal sealed class TrayAppContext : ApplicationContext
{
    /// <summary>How often the schedule is checked.</summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    /// <summary>Wait after starting or resuming, so the spooler, network and printer are ready.</summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    /// <summary>Wait before retrying a failed automatic print.</summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(30);

    private static readonly int[] PresetIntervals = { 1, 2, 3, 4, 5, 7, 10, 14, 21, 30 };

    private readonly SettingsStore _store = SettingsStore.CreateDefault();
    private readonly AppSettings _settings;
    private readonly Icon _icon = LoadIcon("inksaver.ico");
    private readonly Icon _pausedIcon = LoadIcon("inksaver-paused.ico");
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Bitmap _headerImage;
    private readonly Font _headerFont;
    private readonly SynchronizationContext _uiContext;

    private DateTime _notBefore;
    private bool _printing;
    private bool _dialogOpen;

    public TrayAppContext()
    {
        _settings = _store.Load();

        // Creating the menu (a Control) installed the WinForms context for this thread.
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        _headerImage = _icon.ToBitmap();
        _headerFont = new Font(_menu.Font, FontStyle.Bold);
        _menu.Opening += (_, _) => BuildMenu();
        _tray = new NotifyIcon { ContextMenuStrip = _menu, Visible = true };
        _tray.MouseClick += OnTrayClick;

        _timer.Interval = (int)CheckInterval.TotalMilliseconds;
        _timer.Tick += (_, _) => CheckSchedule();
        _timer.Start();

        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        if (_settings.FirstRun is null)
        {
            OnFirstRun();
        }
        else
        {
            TryRefreshStartupPath();
        }

        DelayAutomaticPrint(StartupDelay);
        UpdateTrayState();
    }

    // --- Scheduling ------------------------------------------------------------------------------

    private void OnFirstRun()
    {
        _settings.FirstRun = DateTimeOffset.Now;
        SaveSettings();

        try
        {
            StartupManager.SetEnabled(true);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Could not enable start with Windows: {ex.Message}");
        }

        AppLog.Write("First run.");
        ShowBalloon(ToolTipIcon.Info, Strings.WelcomeTitle, Strings.WelcomeBody(_settings.IntervalDays, CurrentPrinterName()));
    }

    /// <summary>Holds back automatic printing for a while and announces it if a print is due.</summary>
    private void DelayAutomaticPrint(TimeSpan delay)
    {
        _notBefore = DateTime.Now + delay;
        if (Schedule.IsDue(_settings, DateTime.Now))
        {
            ShowBalloon(ToolTipIcon.Info, Strings.PendingTitle, Strings.PendingBody((int)delay.TotalMinutes, CurrentPrinterName()));
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            // SystemEvents may be raised on another thread.
            _uiContext.Post(_ => DelayAutomaticPrint(StartupDelay), null);
        }
    }

    private void CheckSchedule()
    {
        UpdateTrayState();

        var now = DateTime.Now;
        if (!_printing && now >= _notBefore && Schedule.IsDue(_settings, now))
        {
            _ = PrintAsync(automatic: true);
        }
    }

    private async Task PrintAsync(bool automatic)
    {
        if (_printing)
        {
            return;
        }

        _printing = true;
        UpdateTrayState();
        var source = _settings.Source == PrintSource.PdfFile ? $"PDF \"{_settings.PdfPath}\"" : "test page";
        try
        {
            using var document = await MaintenancePrint.CreateDocumentAsync(_settings);
            var printerName = document.PrinterSettings.PrinterName;
            await Task.Run(document.Print);

            _settings.LastPrint = DateTimeOffset.Now;
            SaveSettings();
            AppLog.Write($"{(automatic ? "Automatic" : "Manual")} print of {source} sent to \"{printerName}\".");
            ShowBalloon(ToolTipIcon.Info, Strings.PrintedTitle, Strings.PrintedBody(printerName, Schedule.NextDue(_settings, DateTime.Now)));
        }
        catch (Exception ex)
        {
            AppLog.Write($"{(automatic ? "Automatic" : "Manual")} print of {source} FAILED: {ex}");
            var message = ex is PrintException ? ex.Message : $"{ex.GetType().Name}: {ex.Message}";
            if (automatic)
            {
                _notBefore = DateTime.Now + RetryDelay;
                message += " " + Strings.RetryIn((int)RetryDelay.TotalMinutes);
            }

            ShowBalloon(ToolTipIcon.Error, Strings.PrintFailedTitle, message);
        }
        finally
        {
            _printing = false;
            UpdateTrayState();
        }
    }

    // --- Tray icon and menu ----------------------------------------------------------------------

    private void OnTrayClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // NotifyIcon only opens its menu on right click; reuse the same (internal) code path on left click.
            typeof(NotifyIcon)
                .GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(_tray, null);
        }
    }

    private string StatusText()
    {
        if (_printing)
        {
            return Strings.PrintingNow;
        }

        if (_settings.Paused)
        {
            return Strings.PausedStatus;
        }

        return Schedule.IsDue(_settings, DateTime.Now)
            ? Strings.PrintPending
            : Strings.NextPrint(Schedule.NextDue(_settings, DateTime.Now));
    }

    private void UpdateTrayState()
    {
        var icon = _settings.Paused ? _pausedIcon : _icon;
        if (!ReferenceEquals(_tray.Icon, icon))
        {
            _tray.Icon = icon;
        }

        var text = $"{Strings.AppName}\n{StatusText()}";
        text = text.Length > 127 ? text[..127] : text;
        if (_tray.Text != text)
        {
            _tray.Text = text;
        }
    }

    private void BuildMenu()
    {
        // Rebuilt on every opening so printers, dates and check marks are always current.
        foreach (var item in _menu.Items.Cast<ToolStripItem>().ToList())
        {
            item.Dispose();
        }

        _menu.Items.Add(new ToolStripMenuItem(Strings.AppName) { Enabled = false, Image = _headerImage, Font = _headerFont });
        _menu.Items.Add(new ToolStripMenuItem(StatusText()) { Enabled = false });
        _menu.Items.Add(new ToolStripMenuItem(Strings.LastPrint(_settings.LastPrint)) { Enabled = false });
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(new ToolStripMenuItem(Strings.PrintNow, null, (_, _) => _ = PrintAsync(automatic: false))
        {
            Enabled = !_printing,
        });
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(BuildPrinterMenu());
        _menu.Items.Add(BuildFrequencyMenu());
        _menu.Items.Add(BuildSourceMenu());
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(new ToolStripMenuItem(Strings.PauseAutomaticPrinting, null, (_, _) => TogglePaused())
        {
            Checked = _settings.Paused,
        });
        _menu.Items.Add(new ToolStripMenuItem(Strings.StartWithWindows, null, (_, _) => ToggleStartup())
        {
            Checked = SafeIsStartupEnabled(),
        });
        _menu.Items.Add(new ToolStripMenuItem(Strings.ShowHistory, null, (_, _) => OpenHistory())
        {
            Enabled = File.Exists(AppLog.FilePath),
        });
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem(Strings.Exit, null, (_, _) => ExitThread()));
    }

    private ToolStripMenuItem BuildPrinterMenu()
    {
        var menu = new ToolStripMenuItem(Strings.PrinterMenu);

        menu.DropDownItems.Add(new ToolStripMenuItem(
            Strings.DefaultPrinter(new PrinterSettings().PrinterName), null, (_, _) => SetPrinter(null))
        {
            Checked = _settings.PrinterName is null,
        });
        menu.DropDownItems.Add(new ToolStripSeparator());

        var installed = PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList();
        foreach (var name in installed)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem(name, null, (_, _) => SetPrinter(name))
            {
                Checked = string.Equals(_settings.PrinterName, name, StringComparison.OrdinalIgnoreCase),
            });
        }

        if (_settings.PrinterName is { } configured && !installed.Contains(configured, StringComparer.OrdinalIgnoreCase))
        {
            menu.DropDownItems.Add(new ToolStripMenuItem(Strings.UnavailablePrinter(configured)) { Checked = true, Enabled = false });
        }
        else if (installed.Count == 0)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem(Strings.NoPrintersInstalled) { Enabled = false });
        }

        return menu;
    }

    private ToolStripMenuItem BuildFrequencyMenu()
    {
        var menu = new ToolStripMenuItem(Strings.FrequencyMenu);
        foreach (var days in PresetIntervals)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem(Strings.EveryDays(days), null, (_, _) => SetInterval(days))
            {
                Checked = _settings.IntervalDays == days,
            });
        }

        menu.DropDownItems.Add(new ToolStripSeparator());
        var isCustom = !PresetIntervals.Contains(_settings.IntervalDays);
        menu.DropDownItems.Add(new ToolStripMenuItem(
            isCustom ? Strings.CustomFrequencySelected(_settings.IntervalDays) : Strings.CustomFrequency,
            null,
            (_, _) => ChooseCustomInterval())
        {
            Checked = isCustom,
        });

        return menu;
    }

    private ToolStripMenuItem BuildSourceMenu()
    {
        var menu = new ToolStripMenuItem(Strings.SourceMenu);
        menu.DropDownItems.Add(new ToolStripMenuItem(Strings.GeneratedPage, null, (_, _) => SetSource(PrintSource.GeneratedPage, _settings.PdfPath))
        {
            Checked = _settings.Source == PrintSource.GeneratedPage,
        });

        var usingPdf = _settings.Source == PrintSource.PdfFile && !string.IsNullOrEmpty(_settings.PdfPath);
        menu.DropDownItems.Add(new ToolStripMenuItem(
            usingPdf ? Strings.PdfSelected(Path.GetFileName(_settings.PdfPath!)) : Strings.ChoosePdf,
            null,
            async (_, _) => await ChoosePdfAsync())
        {
            Checked = usingPdf,
            ToolTipText = usingPdf ? _settings.PdfPath : null,
        });

        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add(new ToolStripMenuItem(Strings.Preview, null, async (_, _) => await ShowPreviewAsync()));
        return menu;
    }

    // --- Menu actions ----------------------------------------------------------------------------

    private void SetPrinter(string? name)
    {
        _settings.PrinterName = name;
        SaveSettings();
        AppLog.Write($"Printer set to {(name is null ? "Windows default" : $"\"{name}\"")}.");
        UpdateTrayState();
    }

    private void SetInterval(int days)
    {
        _settings.IntervalDays = days;
        SaveSettings();
        AppLog.Write($"Interval set to {_settings.IntervalDays} day(s).");
        UpdateTrayState();
    }

    private void SetSource(PrintSource source, string? pdfPath)
    {
        _settings.Source = source;
        _settings.PdfPath = pdfPath;
        SaveSettings();
        AppLog.Write(source == PrintSource.PdfFile ? $"Source set to PDF \"{pdfPath}\"." : "Source set to generated test page.");
    }

    private void ChooseCustomInterval()
    {
        if (!BeginDialog())
        {
            return;
        }

        try
        {
            using var dialog = new IntervalDialog(_settings.IntervalDays, _icon);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SetInterval(dialog.Days);
            }
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private async Task ChoosePdfAsync()
    {
        if (!BeginDialog())
        {
            return;
        }

        try
        {
            using var dialog = new OpenFileDialog
            {
                Title = Strings.ChoosePdfTitle,
                Filter = Strings.PdfFilter,
                CheckFileExists = true,
                FileName = _settings.PdfPath ?? string.Empty,
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // Make sure Windows can actually render it before accepting it.
            using (await PdfRasterizer.RenderFirstPageAsync(dialog.FileName, dpi: 24))
            {
            }

            SetSource(PrintSource.PdfFile, dialog.FileName);
        }
        catch (Exception ex)
        {
            ShowError(ex is PrintException ? ex.Message : Strings.PdfUnreadable(ex.Message));
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private async Task ShowPreviewAsync()
    {
        if (!BeginDialog())
        {
            return;
        }

        try
        {
            using var document = await MaintenancePrint.CreateDocumentAsync(_settings);
            using var preview = new PrintPreviewDialog
            {
                Document = document,
                Text = Strings.PreviewTitle,
                Icon = _icon,
                TopMost = true,
                WindowState = FormWindowState.Maximized,
                UseAntiAlias = true,
            };
            preview.ShowDialog();
        }
        catch (Exception ex)
        {
            ShowError(ex is PrintException ? ex.Message : $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private void TogglePaused()
    {
        _settings.Paused = !_settings.Paused;
        SaveSettings();
        AppLog.Write(_settings.Paused ? "Automatic printing paused." : "Automatic printing resumed.");
        UpdateTrayState();
    }

    private void ToggleStartup()
    {
        try
        {
            var enable = !StartupManager.IsEnabled;
            StartupManager.SetEnabled(enable);
            AppLog.Write(enable ? "Start with Windows enabled." : "Start with Windows disabled.");
        }
        catch (Exception ex)
        {
            ShowError(Strings.StartupChangeFailed(ex.Message));
        }
    }

    private static void OpenHistory()
    {
        Process.Start(new ProcessStartInfo(AppLog.FilePath) { UseShellExecute = true });
    }

    // --- Helpers ---------------------------------------------------------------------------------

    private bool BeginDialog()
    {
        if (_dialogOpen)
        {
            return false;
        }

        _dialogOpen = true;
        return true;
    }

    private string CurrentPrinterName() =>
        MaintenancePrint.ResolvePrinterName(_settings.PrinterName) is { Length: > 0 } name ? name : Strings.DefaultPrinter(null);

    private void SaveSettings()
    {
        try
        {
            _store.Save(_settings);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Saving settings failed: {ex.Message}");
            ShowBalloon(ToolTipIcon.Error, Strings.AppName, Strings.SettingsSaveFailed(ex.Message));
        }
    }

    private static bool SafeIsStartupEnabled()
    {
        try
        {
            return StartupManager.IsEnabled;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void TryRefreshStartupPath()
    {
        try
        {
            StartupManager.RefreshPathIfEnabled();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Could not update start with Windows entry: {ex.Message}");
        }
    }

    private void ShowBalloon(ToolTipIcon icon, string title, string text) =>
        _tray.ShowBalloonTip(10_000, title, text, icon);

    private static void ShowError(string message) =>
        MessageBox.Show(message, Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private static Icon LoadIcon(string name)
    {
        using var stream = typeof(TrayAppContext).Assembly.GetManifestResourceStream($"InkSaver.{name}")
            ?? throw new InvalidOperationException($"Missing embedded icon {name}.");
        return new Icon(stream, SystemInformation.SmallIconSize);
    }

    protected override void ExitThreadCore()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _timer.Stop();
        _tray.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _tray.Dispose();
            _menu.Dispose();
            _headerImage.Dispose();
            _headerFont.Dispose();
            _icon.Dispose();
            _pausedIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
