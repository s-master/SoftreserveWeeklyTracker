using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Models.Entities;

public class SessionReservationResult
{
    public int Id { get; set; }
    public int RaidSessionId { get; set; }
    public int PlayerId { get; set; }
    public int ItemId { get; set; }
    public bool ItemDropped { get; set; }
    public bool PlayerReceived { get; set; }
    public int PlusOneDelta { get; set; }
    public PlusOneReason Reason { get; set; }
    public int? AwardedToPlayerId { get; set; }

    public RaidSession RaidSession { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public Player? AwardedToPlayer { get; set; }
}
