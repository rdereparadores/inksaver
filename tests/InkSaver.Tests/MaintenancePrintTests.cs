using System.Drawing.Printing;
using InkSaver.Core;

namespace InkSaver.Tests;

public sealed class MaintenancePrintTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("inksaver-tests-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task CreateDocument_FailsClearlyForUnknownPrinter()
    {
        var settings = new AppSettings { PrinterName = "InkSaver printer that does not exist" };

        var ex = await Assert.ThrowsAsync<PrintException>(() => MaintenancePrint.CreateDocumentAsync(settings));
        Assert.Contains(settings.PrinterName, ex.Message);
    }

    [Fact]
    public async Task CreateDocument_FailsClearlyForMissingPdf()
    {
        var settings = new AppSettings { Source = PrintSource.PdfFile, PdfPath = Path.Combine(_directory, "missing.pdf") };

        var ex = await Assert.ThrowsAsync<PrintException>(() => MaintenancePrint.CreateDocumentAsync(settings));
        Assert.Contains("missing.pdf", ex.Message);
    }

    [Theory]
    [InlineData(PrintSource.GeneratedPage)]
    [InlineData(PrintSource.PdfFile)]
    public async Task Document_RendersOnePage(PrintSource source)
    {
        var printer = PrinterSettings.InstalledPrinters.Cast<string>().FirstOrDefault();
        if (printer is null)
        {
            return; // No printer on this machine: nothing to render against.
        }

        var settings = new AppSettings { PrinterName = printer, Source = source, PdfPath = TestPdf.Create(_directory) };
        using var document = await MaintenancePrint.CreateDocumentAsync(settings);

        // Render into memory instead of sending anything to the printer.
        var controller = new PreviewPrintController();
        document.PrintController = controller;
        document.Print();

        var pages = controller.GetPreviewPageInfo();
        Assert.Single(pages);
        foreach (var page in pages)
        {
            page.Image.Dispose();
        }
    }
}
