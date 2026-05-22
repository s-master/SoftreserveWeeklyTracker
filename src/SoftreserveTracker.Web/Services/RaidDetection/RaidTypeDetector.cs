using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Services.RaidDetection;

public static class RaidTypeDetector
{
    private static readonly HashSet<string> TkBosses =
    [
        "al'ar", "alar", "void reaver", "high astromancer solarian",
        "kae'thas sunstrider", "kael'thas sunstrider"
    ];

    private static readonly HashSet<string> SscBosses =
    [
        "hydross the unstable", "the lurker below", "leotheras the blind",
        "fathom-lord karathress", "morogrim tidewalker", "lady vashj", "trash"
    ];

    public static RaidType? DetectFromBoss(string? bossName)
    {
        if (string.IsNullOrWhiteSpace(bossName))
        {
            return null;
        }

        var normalized = bossName.Trim().ToLowerInvariant();
        if (TkBosses.Any(b => normalized.Contains(b, StringComparison.Ordinal)))
        {
            return RaidType.Tk;
        }

        if (SscBosses.Any(b => normalized.Contains(b, StringComparison.Ordinal)))
        {
            return RaidType.Ssc;
        }

        return null;
    }

    public static RaidType DetectFromBosses(IEnumerable<string?> bossNames)
    {
        var counts = new Dictionary<RaidType, int>();
        foreach (var boss in bossNames)
        {
            var type = DetectFromBoss(boss);
            if (type.HasValue)
            {
                counts[type.Value] = counts.GetValueOrDefault(type.Value) + 1;
            }
        }

        if (counts.Count == 0)
        {
            return RaidType.Ssc;
        }

        return counts.OrderByDescending(x => x.Value).First().Key;
    }
}
