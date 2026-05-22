namespace SoftreserveTracker.Web.Services.RaidWindow;

/// <summary>
/// Raid window: Wednesday 05:00 until next Wednesday 03:00 (server/local time).
/// </summary>
public static class RaidWindowCalculator
{
    public static RaidWindow GetWindowForDateTime(DateTime localDateTime)
    {
        var wednesdayStart = FindWindowStart(localDateTime);
        var windowEnd = wednesdayStart.AddDays(7).AddHours(-2); // next Wed 03:00
        return new RaidWindow(wednesdayStart, windowEnd);
    }

    private static DateTime FindWindowStart(DateTime dt)
    {
        // Walk back to the most recent Wednesday 05:00 that is <= dt
        var candidate = new DateTime(dt.Year, dt.Month, dt.Day, 5, 0, 0, dt.Kind);
        while (candidate.DayOfWeek != DayOfWeek.Wednesday)
        {
            candidate = candidate.AddDays(-1);
        }

        if (dt < candidate)
        {
            candidate = candidate.AddDays(-7);
        }

        return candidate;
    }
}

public readonly record struct RaidWindow(DateTime PeriodStart, DateTime PeriodEnd)
{
    public bool Contains(DateTime dt) => dt >= PeriodStart && dt < PeriodEnd;
}
