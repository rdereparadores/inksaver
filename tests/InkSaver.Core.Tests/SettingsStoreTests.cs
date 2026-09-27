using InkSaver.Core;

namespace InkSaver.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "inksaver-tests-" + Guid.NewGuid());

    private SettingsStore CreateStore() => new(Path.Combine(_directory, "nested", "settings.json"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileIsMissing()
    {
        var settings = CreateStore().Load();

        Assert.Equal(AppSettings.DefaultIntervalDays, settings.IntervalDays);
        Assert.Null(settings.PrinterName);
        Assert.Equal(PrintSource.GeneratedPage, settings.Source);
        Assert.Null(settings.LastPrint);
        Assert.False(settings.Paused);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllValues()
    {
        var store = CreateStore();
        var original = new AppSettings
        {
            IntervalDays = 4,
            PrinterName = "EPSON ET-2810 Series",
            Source = PrintSource.PdfFile,
            PdfPath = @"C:\Users\me\Documents\nozzle.pdf",
            LastPrint = new DateTimeOffset(2026, 9, 20, 10, 15, 0, TimeSpan.FromHours(2)),
            FirstRun = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.FromHours(2)),
            Paused = true,
        };

        store.Save(original);
        var loaded = store.Load();

        Assert.Equal(original.IntervalDays, loaded.IntervalDays);
        Assert.Equal(original.PrinterName, loaded.PrinterName);
        Assert.Equal(original.Source, loaded.Source);
        Assert.Equal(original.PdfPath, loaded.PdfPath);
        Assert.Equal(original.LastPrint, loaded.LastPrint);
        Assert.Equal(original.FirstRun, loaded.FirstRun);
        Assert.True(loaded.Paused);
        Assert.False(File.Exists(store.FilePath + ".tmp"));
    }

    [Fact]
    public void Save_StoresSourceAsReadableText()
    {
        var store = CreateStore();
        store.Save(new AppSettings { Source = PrintSource.PdfFile });

        Assert.Contains("\"PdfFile\"", File.ReadAllText(store.FilePath));
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileIsCorrupt()
    {
        var store = CreateStore();
        Directory.CreateDirectory(Path.GetDirectoryName(store.FilePath)!);
        File.WriteAllText(store.FilePath, "{ this is not json");

        Assert.Equal(AppSettings.DefaultIntervalDays, store.Load().IntervalDays);
    }
}
