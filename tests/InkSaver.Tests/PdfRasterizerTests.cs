namespace InkSaver.Tests;

public sealed class PdfRasterizerTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("inksaver-tests-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task RenderFirstPage_ProducesBitmapWithPageContent()
    {
        var path = TestPdf.Create(_directory);

        using var bitmap = await PdfRasterizer.RenderFirstPageAsync(path, dpi: 72);

        // 612 x 792 pt at 72 dpi.
        Assert.InRange(bitmap.Width, 608, 616);
        Assert.InRange(bitmap.Height, 788, 796);

        var centre = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
        Assert.True(centre.R > 200 && centre.G < 60 && centre.B < 60, $"Expected red, got {centre}");

        var corner = bitmap.GetPixel(5, 5);
        Assert.True(corner.R > 240 && corner.G > 240 && corner.B > 240, $"Expected white background, got {corner}");
    }

    [Fact]
    public async Task RenderFirstPage_RejectsFilesThatAreNotPdf()
    {
        var path = Path.Combine(_directory, "not-a-pdf.pdf");
        await File.WriteAllTextAsync(path, "hello");

        await Assert.ThrowsAsync<PrintException>(() => PdfRasterizer.RenderFirstPageAsync(path));
    }
}
