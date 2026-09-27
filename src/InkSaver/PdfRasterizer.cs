using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace InkSaver;

/// <summary>Renders PDF pages with the PDF engine built into Windows 10/11.</summary>
internal static class PdfRasterizer
{
    /// <summary>Renders the first page of <paramref name="path"/> at roughly <paramref name="dpi"/>.</summary>
    public static async Task<Bitmap> RenderFirstPageAsync(string path, int dpi = 200)
    {
        PdfDocument pdf;
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(path));
            pdf = await PdfDocument.LoadFromFileAsync(file);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new PrintException(Strings.PdfUnreadable(ex.Message), ex);
        }

        if (pdf.PageCount == 0)
        {
            throw new PrintException(Strings.PdfHasNoPages);
        }

        using var page = pdf.GetPage(0);

        // Page size is in DIPs (1/96 inch). Only the width is given, so the aspect ratio is kept.
        var options = new PdfPageRenderOptions
        {
            DestinationWidth = (uint)Math.Max(1, Math.Round(page.Size.Width / 96.0 * dpi)),
            BackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255),
        };

        using var stream = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(stream, options);

        var bytes = new byte[stream.Size];
        using (var reader = new DataReader(stream.GetInputStreamAt(0)))
        {
            await reader.LoadAsync((uint)stream.Size);
            reader.ReadBytes(bytes);
        }

        using var memory = new MemoryStream(bytes);
        using var decoded = new Bitmap(memory);
        return new Bitmap(decoded); // Detach from the stream, which GDI+ would otherwise need to keep open.
    }
}
