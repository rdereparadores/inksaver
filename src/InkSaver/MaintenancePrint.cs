using System.Drawing.Printing;
using InkSaver.Core;

namespace InkSaver;

/// <summary>Builds the <see cref="PrintDocument"/> for one maintenance page.</summary>
internal static class MaintenancePrint
{
    /// <summary>Extra white border around the generated page, in hundredths of an inch.</summary>
    private const float GeneratedPageMargin = 40f;

    /// <summary>
    /// Creates a one-page document for the configured printer and source.
    /// Throws <see cref="PrintException"/> with a user-friendly message when that is not possible.
    /// </summary>
    public static async Task<PrintDocument> CreateDocumentAsync(AppSettings settings)
    {
        Bitmap? pdfPage = null;
        if (settings.Source == PrintSource.PdfFile)
        {
            if (string.IsNullOrWhiteSpace(settings.PdfPath))
            {
                throw new PrintException(Strings.PdfNotChosen);
            }

            if (!File.Exists(settings.PdfPath))
            {
                throw new PrintException(Strings.PdfNotFound(settings.PdfPath));
            }

            pdfPage = await PdfRasterizer.RenderFirstPageAsync(settings.PdfPath);
        }

        var document = new PrintDocument
        {
            DocumentName = "InkSaver",
            PrintController = new StandardPrintController(), // no "Printing page 1…" window
        };

        try
        {
            ConfigurePrinter(document, settings.PrinterName);
        }
        catch
        {
            pdfPage?.Dispose();
            document.Dispose();
            throw;
        }

        document.PrintPage += (_, e) =>
        {
            var area = e.Graphics!.VisibleClipBounds; // printable area, in 1/100 inch
            if (pdfPage is not null)
            {
                DrawFitted(e.Graphics, pdfPage, area);
            }
            else
            {
                area.Inflate(-GeneratedPageMargin, -GeneratedPageMargin);
                TestPage.Draw(e.Graphics, area, document.PrinterSettings.PrinterName, DateTime.Now);
            }

            e.HasMorePages = false;
        };
        document.Disposed += (_, _) => pdfPage?.Dispose();

        return document;
    }

    /// <summary>Name of the printer that will be used: the chosen one, or the Windows default.</summary>
    public static string ResolvePrinterName(string? configuredName) =>
        configuredName ?? new PrinterSettings().PrinterName;

    private static void ConfigurePrinter(PrintDocument document, string? printerName)
    {
        if (printerName is not null)
        {
            document.PrinterSettings.PrinterName = printerName;
            if (!document.PrinterSettings.IsValid)
            {
                throw new PrintException(Strings.PrinterNotFound(printerName));
            }
        }
        else if (string.IsNullOrEmpty(document.PrinterSettings.PrinterName) || !document.PrinterSettings.IsValid)
        {
            throw new PrintException(Strings.NoDefaultPrinter);
        }

        document.PrinterSettings.Copies = 1;
        if (document.PrinterSettings.SupportsColor)
        {
            document.DefaultPageSettings.Color = true;
        }
    }

    /// <summary>Draws the image as large as possible inside the area, rotating it if that fits better.</summary>
    private static void DrawFitted(Graphics g, Bitmap image, RectangleF area)
    {
        var imageLandscape = image.Width > image.Height;
        var areaLandscape = area.Width > area.Height;
        if (imageLandscape != areaLandscape)
        {
            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
        }

        var scale = Math.Min(area.Width / image.Width, area.Height / image.Height);
        var width = image.Width * scale;
        var height = image.Height * scale;
        var target = new RectangleF(
            area.Left + (area.Width - width) / 2,
            area.Top + (area.Height - height) / 2,
            width,
            height);

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(image, target);
    }
}
