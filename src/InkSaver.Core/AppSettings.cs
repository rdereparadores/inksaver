using System.Text.Json.Serialization;

namespace InkSaver.Core;

/// <summary>What gets printed on each maintenance run.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PrintSource>))]
public enum PrintSource
{
    /// <summary>A colour test page drawn by InkSaver itself.</summary>
    GeneratedPage,

    /// <summary>The first page of a PDF chosen by the user.</summary>
    PdfFile,
}

/// <summary>User configuration and schedule state, persisted as JSON.</summary>
public sealed class AppSettings
{
    public const int DefaultIntervalDays = 7;
    public const int MinIntervalDays = 1;
    public const int MaxIntervalDays = 365;

    private int _intervalDays = DefaultIntervalDays;

    /// <summary>Days between two maintenance prints.</summary>
    public int IntervalDays
    {
        get => _intervalDays;
        set => _intervalDays = Math.Clamp(value, MinIntervalDays, MaxIntervalDays);
    }

    /// <summary>Printer to use; <c>null</c> means the Windows default printer.</summary>
    public string? PrinterName { get; set; }

    public PrintSource Source { get; set; } = PrintSource.GeneratedPage;

    /// <summary>Full path of the PDF used when <see cref="Source"/> is <see cref="PrintSource.PdfFile"/>.</summary>
    public string? PdfPath { get; set; }

    /// <summary>When the last successful maintenance print was sent.</summary>
    public DateTimeOffset? LastPrint { get; set; }

    /// <summary>When InkSaver first ran; the schedule starts from here until the first print.</summary>
    public DateTimeOffset? FirstRun { get; set; }

    /// <summary>Automatic printing is suspended while this is set.</summary>
    public bool Paused { get; set; }
}
