using InkSaver.Core;

namespace InkSaver.Core.Tests;

public class ScheduleTests
{
    private static DateTimeOffset Local(int year, int month, int day, int hour = 0, int minute = 0) =>
        new(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local));

    [Fact]
    public void NextDue_CountsCalendarDaysFromLastPrint()
    {
        var settings = new AppSettings { IntervalDays = 3, LastPrint = Local(2026, 9, 7, 18, 30) };

        var due = Schedule.NextDue(settings, new DateTime(2026, 9, 8));

        Assert.Equal(new DateTime(2026, 9, 10), due);
    }

    [Fact]
    public void NextDue_UsesFirstRunUntilSomethingIsPrinted()
    {
        var settings = new AppSettings { IntervalDays = 7, FirstRun = Local(2026, 9, 1, 12) };

        Assert.Equal(new DateTime(2026, 9, 8), Schedule.NextDue(settings, new DateTime(2026, 9, 2)));
    }

    [Fact]
    public void NextDue_LastPrintTakesPrecedenceOverFirstRun()
    {
        var settings = new AppSettings
        {
            IntervalDays = 2,
            FirstRun = Local(2026, 1, 1),
            LastPrint = Local(2026, 9, 20, 9),
        };

        Assert.Equal(new DateTime(2026, 9, 22), Schedule.NextDue(settings, new DateTime(2026, 9, 21)));
    }

    [Fact]
    public void IsDue_FalseBeforeDueDay()
    {
        var settings = new AppSettings { IntervalDays = 3, LastPrint = Local(2026, 9, 7, 18) };

        Assert.False(Schedule.IsDue(settings, new DateTime(2026, 9, 9, 23, 59, 59)));
    }

    [Fact]
    public void IsDue_TrueFromMidnightOfDueDay()
    {
        var settings = new AppSettings { IntervalDays = 3, LastPrint = Local(2026, 9, 7, 18) };

        Assert.True(Schedule.IsDue(settings, new DateTime(2026, 9, 10, 0, 0, 0)));
    }

    [Fact]
    public void IsDue_TrueWhenComputerWasOffPastTheDueDate()
    {
        var settings = new AppSettings { IntervalDays = 5, LastPrint = Local(2026, 8, 1, 10) };

        Assert.True(Schedule.IsDue(settings, new DateTime(2026, 9, 27, 8, 15, 0)));
    }

    [Fact]
    public void IsDue_FalseWhilePaused()
    {
        var settings = new AppSettings { IntervalDays = 1, LastPrint = Local(2026, 8, 1), Paused = true };

        Assert.False(Schedule.IsDue(settings, new DateTime(2026, 9, 27)));
    }

    [Theory]
    [InlineData(0, AppSettings.MinIntervalDays)]
    [InlineData(-4, AppSettings.MinIntervalDays)]
    [InlineData(10, 10)]
    [InlineData(1000, AppSettings.MaxIntervalDays)]
    public void IntervalDays_IsClamped(int value, int expected)
    {
        Assert.Equal(expected, new AppSettings { IntervalDays = value }.IntervalDays);
    }
}
