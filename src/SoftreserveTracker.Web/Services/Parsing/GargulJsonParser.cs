using System.Text.Json;

namespace SoftreserveTracker.Web.Services.Parsing;

public sealed class GargulJsonParser : IGargulJsonParser
{
    public GargulParseResult Parse(string jsonContent)
    {
        var items = JsonSerializer.Deserialize<List<GargulJsonItem>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Gargul JSON is empty or invalid.");

        if (items.Count == 0)
        {
            throw new InvalidOperationException("Gargul JSON contains no loot entries.");
        }

        var entries = items.Select(MapEntry).ToList();
        var groups = entries
            .GroupBy(e => e.SoftresId ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sessionDate = entries.Min(e => e.AwardedAt).Date;

        return new GargulParseResult
        {
            GroupsBySoftresId = groups,
            SessionDate = sessionDate
        };
    }

    public IReadOnlySet<DateTime> GetSessionDates(string jsonContent) =>
        Parse(jsonContent).GroupsBySoftresId.Values
            .SelectMany(g => g)
            .Select(e => e.AwardedAt.Date)
            .ToHashSet();

    private static GargulLootEntry MapEntry(GargulJsonItem item)
    {
        var awardedTo = item.AwardedTo ?? string.Empty;
        var isDe = awardedTo.Contains("|de|", StringComparison.OrdinalIgnoreCase);
        var playerName = ExtractPlayerName(awardedTo);

        return new GargulLootEntry
        {
            ItemId = item.ItemID,
            ItemLink = item.ItemLink ?? string.Empty,
            AwardedToRaw = awardedTo,
            PlayerName = playerName,
            SoftReserveWin = item.SR,
            SoftresId = string.IsNullOrWhiteSpace(item.SoftresID) ? null : item.SoftresID,
            AwardedAt = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp).LocalDateTime,
            IsDisenchanted = isDe,
            Rolls = MapRolls(item.Rolls)
        };
    }

    private static IReadOnlyList<GargulRollEntry> MapRolls(List<GargulJsonRoll>? rolls)
    {
        if (rolls == null || rolls.Count == 0)
        {
            return [];
        }

        return rolls
            .Where(r => !string.IsNullOrWhiteSpace(r.Player))
            .Select(r => new GargulRollEntry
            {
                PlayerName = r.Player!.Trim(),
                RollAmount = r.Amount,
                PlayerClass = r.Class?.Trim(),
                Classification = r.Classification?.Trim(),
                Priority = r.Priority,
                PlusOneState = r.PlusOneState,
                RolledAt = DateTimeOffset.FromUnixTimeSeconds(r.Time).LocalDateTime
            })
            .ToList();
    }

    private static string? ExtractPlayerName(string awardedTo)
    {
        if (string.IsNullOrWhiteSpace(awardedTo) || awardedTo.Contains("|de|", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var dashIndex = awardedTo.IndexOf('-');
        return dashIndex > 0 ? awardedTo[..dashIndex] : awardedTo;
    }

    private sealed class GargulJsonItem
    {
        public int ItemID { get; set; }
        public string? ItemLink { get; set; }
        public string? AwardedTo { get; set; }
        public bool SR { get; set; }
        public string? SoftresID { get; set; }
        public long Timestamp { get; set; }
        public List<GargulJsonRoll>? Rolls { get; set; }
    }

    private sealed class GargulJsonRoll
    {
        public int Amount { get; set; }
        public string? Class { get; set; }
        public string? Classification { get; set; }
        public string? Player { get; set; }
        public int? Priority { get; set; }
        public int? PlusOneState { get; set; }
        public long Time { get; set; }
    }
}
