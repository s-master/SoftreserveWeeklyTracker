namespace SoftreserveTracker.Web.Models.Entities;

public class Roster
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid AccessToken { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<RaidWeek> RaidWeeks { get; set; } = [];
    public ICollection<Player> Players { get; set; } = [];
}
