using System.Drawing.Drawing2D;

namespace InkSaver;

/// <summary>
/// Draws the built-in maintenance page: solid blocks of every cartridge colour,
/// mixes, gradients and a fine line pattern so every nozzle fires a little ink.
/// Coordinates are in hundredths of an inch (the default printer page unit).
/// </summary>
internal static class TestPage
{
    private static readonly Color Cyan = Color.FromArgb(0, 255, 255);
    private static readonly Color Magenta = Color.FromArgb(255, 0, 255);
    private static readonly Color Yellow = Color.FromArgb(255, 255, 0);
    private static readonly Color Black = Color.FromArgb(0, 0, 0);

    private static readonly (Color Colour, string Label)[] Primaries =
    {
        (Cyan, "C"), (Magenta, "M"), (Yellow, "Y"), (Black, "K"),
    };

    private static readonly (Color Colour, string Label)[] Mixes =
    {
        (Color.FromArgb(255, 0, 0), "R"),
        (Color.FromArgb(0, 160, 0), "G"),
        (Color.FromArgb(0, 0, 255), "B"),
        (Color.FromArgb(128, 128, 128), "50%"),
    };

    public static void Draw(Graphics g, RectangleF area, string printerName, DateTime printedAt)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        const float Gap = 12f;
        var y = area.Top;

        using var titleFont = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Point);
        using var textFont = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        using var sectionFont = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Point);
        using var labelFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point);
        using var textBrush = new SolidBrush(Color.FromArgb(40, 40, 40));

        y = DrawText(g, "InkSaver", titleFont, textBrush, area, y);
        y = DrawText(g, Strings.TestPageTitle, sectionFont, textBrush, area, y);
        y = DrawText(g, Strings.TestPagePrintedOn(printedAt, printerName), textFont, textBrush, area, y) + 25f;

        y = DrawText(g, Strings.TestPagePrimaryColours, sectionFont, textBrush, area, y) + 4f;
        y = DrawBlocks(g, Primaries, area.Left, y, area.Width, 110f, Gap, labelFont) + 25f;

        y = DrawText(g, Strings.TestPageSecondaryColours, sectionFont, textBrush, area, y) + 4f;
        y = DrawBlocks(g, Mixes, area.Left, y, area.Width, 60f, Gap, labelFont) + 25f;

        y = DrawText(g, Strings.TestPageGradients, sectionFont, textBrush, area, y) + 4f;
        foreach (var (colour, _) in Primaries)
        {
            var strip = new RectangleF(area.Left, y, area.Width, 22f);
            using var brush = new LinearGradientBrush(strip, Color.White, colour, LinearGradientMode.Horizontal);
            g.FillRectangle(brush, strip);
            y += strip.Height + 6f;
        }

        y += 19f;
        y = DrawText(g, Strings.TestPageNozzles, sectionFont, textBrush, area, y) + 4f;
        y = DrawNozzlePattern(g, area.Left, y, area.Width, Gap) + 25f;

        var footerHeight = g.MeasureString(Strings.TestPageFooter, textFont, (int)area.Width).Height;
        DrawText(g, Strings.TestPageFooter, textFont, textBrush, area, Math.Max(y, area.Bottom - footerHeight));
    }

    private static float DrawText(Graphics g, string text, Font font, Brush brush, RectangleF area, float y)
    {
        var size = g.MeasureString(text, font, (int)area.Width);
        g.DrawString(text, font, brush, new RectangleF(area.Left, y, area.Width, size.Height));
        return y + size.Height;
    }

    private static float DrawBlocks(
        Graphics g, (Color Colour, string Label)[] blocks, float left, float top, float width, float height, float gap, Font font)
    {
        var blockWidth = (width - gap * (blocks.Length - 1)) / blocks.Length;
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (var i = 0; i < blocks.Length; i++)
        {
            var rect = new RectangleF(left + i * (blockWidth + gap), top, blockWidth, height);
            using var fill = new SolidBrush(blocks[i].Colour);
            g.FillRectangle(fill, rect);

            var labelColour = blocks[i].Colour.GetBrightness() < 0.45f ? Color.White : Color.Black;
            using var labelBrush = new SolidBrush(labelColour);
            g.DrawString(blocks[i].Label, font, labelBrush, rect, format);
        }

        return top + height;
    }

    /// <summary>Staggered hairlines per colour, similar to a printer's own nozzle check.</summary>
    private static float DrawNozzlePattern(Graphics g, float left, float top, float width, float gap)
    {
        const int Rows = 8;
        const int Steps = 12;
        const float RowSpacing = 4f;

        var blockWidth = (width - gap * (Primaries.Length - 1)) / Primaries.Length;
        var stepWidth = blockWidth / Steps;

        for (var i = 0; i < Primaries.Length; i++)
        {
            using var pen = new Pen(Primaries[i].Colour, 1f);
            var x0 = left + i * (blockWidth + gap);
            for (var step = 0; step < Steps; step++)
            {
                for (var row = 0; row < Rows; row++)
                {
                    var y = top + row * RowSpacing * 2 + (step % 2) * RowSpacing;
                    g.DrawLine(pen, x0 + step * stepWidth, y, x0 + (step + 1) * stepWidth - 1f, y);
                }
            }
        }

        return top + Rows * RowSpacing * 2;
    }
}
