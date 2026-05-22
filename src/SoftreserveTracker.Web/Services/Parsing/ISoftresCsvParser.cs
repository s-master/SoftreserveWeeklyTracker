namespace SoftreserveTracker.Web.Services.Parsing;

public sealed class SoftresRow
{
    public int ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string? BossSource { get; init; }
    public string PlayerName { get; init; } = string.Empty;
    public string? PlayerClass { get; init; }
    public string? Spec { get; init; }
    public string? Note { get; init; }
    public DateTime ReservedAt { get; init; }
}

public sealed class SoftresParseResult
{
    public required List<SoftresRow> Rows { get; init; }
    public DateTime SessionDate { get; init; }
    public Models.Enums.RaidType RaidType { get; init; }
}

public interface ISoftresCsvParser
{
    SoftresParseResult Parse(string csvContent);
}
