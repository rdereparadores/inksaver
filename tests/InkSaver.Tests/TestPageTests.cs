namespace InkSaver.Tests;

public class TestPageTests
{
    [Fact]
    public void Draw_FillsTheCartridgeColourBlocks()
    {
        // A4 at 100 px per inch, which matches the 1/100 inch printer page unit.
        using var bitmap = new Bitmap(827, 1169);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.White);
            TestPage.Draw(g, new RectangleF(40, 40, 747, 1089), "Test printer", new DateTime(2026, 9, 27, 10, 0, 0));
        }

        var colours = Enumerable.Range(0, bitmap.Height)
            .SelectMany(y => new[] { bitmap.GetPixel(120, y), bitmap.GetPixel(310, y), bitmap.GetPixel(500, y), bitmap.GetPixel(690, y) })
            .Select(c => c.ToArgb())
            .ToHashSet();

        Assert.Contains(Color.FromArgb(0, 255, 255).ToArgb(), colours);
        Assert.Contains(Color.FromArgb(255, 0, 255).ToArgb(), colours);
        Assert.Contains(Color.FromArgb(255, 255, 0).ToArgb(), colours);
        Assert.Contains(Color.FromArgb(0, 0, 0).ToArgb(), colours);
    }
}
