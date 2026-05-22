namespace SoftreserveTracker.Web.Services.Parsing;

public sealed class GargulRollEntry
{
    public string PlayerName { get; init; } = string.Empty;
    public int RollAmount { get; init; }
    public string? PlayerClass { get; init; }
    public string? Classification { get; init; }
    public int? Priority { get; init; }
    public int? PlusOneState { get; init; }
    public DateTime RolledAt { get; init; }
}

public sealed class GargulLootEntry
{
    public int ItemId { get; init; }
    public string ItemLink { get; init; } = string.Empty;
    public string AwardedToRaw { get; init; } = string.Empty;
    public string? PlayerName { get; init; }
    public bool SoftReserveWin { get; init; }
    public string? SoftresId { get; init; }
    public DateTime AwardedAt { get; init; }
    public bool IsDisenchanted { get; init; }
    public IReadOnlyList<GargulRollEntry> Rolls { get; init; } = [];
}

public sealed class GargulParseResult
{
    public required Dictionary<string, List<GargulLootEntry>> GroupsBySoftresId { get; init; }
    public DateTime SessionDate { get; init; }
}

public interface IGargulJsonParser
{
    GargulParseResult Parse(string jsonContent);
}
