namespace SoftreserveTracker.Web.Models.Entities;

public class LootRoll
{
    public int Id { get; set; }
    public int LootAwardId { get; set; }
    public int? PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int RollAmount { get; set; }
    public string? PlayerClass { get; set; }
    public string? Classification { get; set; }
    public int? Priority { get; set; }
    public int? PlusOneState { get; set; }
    public DateTime RolledAt { get; set; }

    public LootAward LootAward { get; set; } = null!;
    public Player? Player { get; set; }
}
