namespace SoftreserveTracker.Web.Models.Entities;

public class Player
{
    public int Id { get; set; }
    public Guid RosterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;

    public Roster Roster { get; set; } = null!;
    public ICollection<SoftReserve> SoftReserves { get; set; } = [];
    public ICollection<LootAward> LootAwards { get; set; } = [];
    public ICollection<PlusOneBalance> PlusOneBalances { get; set; } = [];
    public ICollection<SessionReservationResult> ReservationResults { get; set; } = [];
    public ICollection<LootRoll> LootRolls { get; set; } = [];
}
