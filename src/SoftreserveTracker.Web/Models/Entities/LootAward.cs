namespace SoftreserveTracker.Web.Models.Entities;

public class LootAward
{
    public int Id { get; set; }
    public int RaidSessionId { get; set; }
    public int ItemId { get; set; }
    public int? WinnerPlayerId { get; set; }
    public string AwardedToRaw { get; set; } = string.Empty;
    public bool SoftReserveWin { get; set; }
    public bool IsDisenchanted { get; set; }
    public DateTime AwardedAt { get; set; }
    public string? SoftresId { get; set; }

    public RaidSession RaidSession { get; set; } = null!;
    public Player? WinnerPlayer { get; set; }
    public ICollection<LootRoll> Rolls { get; set; } = [];
}
