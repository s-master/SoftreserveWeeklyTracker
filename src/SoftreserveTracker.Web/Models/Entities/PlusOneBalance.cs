namespace SoftreserveTracker.Web.Models.Entities;

public class PlusOneBalance
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int ItemId { get; set; }
    public int CurrentCount { get; set; }

    public Player Player { get; set; } = null!;
}
