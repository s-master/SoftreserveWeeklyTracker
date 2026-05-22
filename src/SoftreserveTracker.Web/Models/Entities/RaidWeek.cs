namespace SoftreserveTracker.Web.Models.Entities;

public class RaidWeek
{
    public int Id { get; set; }
    public Guid RosterId { get; set; }
    public int WeekNumber { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public Roster Roster { get; set; } = null!;
    public ICollection<RaidSession> Sessions { get; set; } = [];
}
