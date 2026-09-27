namespace InkSaver.Core;

/// <summary>
/// Scheduling rules. Intervals are counted in calendar days: with an interval of
/// 3 days and a print on Monday, the next print is due any time from Thursday
/// 00:00 onwards. If the computer is off on that day, the print is simply
/// overdue and happens as soon as InkSaver runs again.
/// </summary>
public static class Schedule
{
    /// <summary>Local time from which the next print is due.</summary>
    public static DateTime NextDue(AppSettings settings, DateTime nowLocal)
    {
        var anchor = settings.LastPrint ?? settings.FirstRun;
        var anchorDate = anchor?.ToLocalTime().Date ?? nowLocal.Date;
        return anchorDate.AddDays(settings.IntervalDays);
    }

    /// <summary>Whether an automatic print should happen now.</summary>
    public static bool IsDue(AppSettings settings, DateTime nowLocal) =>
        !settings.Paused && nowLocal >= NextDue(settings, nowLocal);
}
