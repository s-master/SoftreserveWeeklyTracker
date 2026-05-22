namespace SoftreserveTracker.Web.Models.Entities;

public class SoftReserve
{
    public int Id { get; set; }
    public int RaidSessionId { get; set; }
    public int PlayerId { get; set; }
    public int ItemId { get; set; }
    public string? BossSource { get; set; }
    public string? PlayerClass { get; set; }
    public string? Spec { get; set; }
    public string? Note { get; set; }
    public DateTime ReservedAt { get; set; }

    public RaidSession RaidSession { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
