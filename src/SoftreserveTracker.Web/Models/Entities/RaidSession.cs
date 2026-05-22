using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Models.Entities;

public class RaidSession
{
    public int Id { get; set; }
    public int RaidWeekId { get; set; }
    public RaidType RaidType { get; set; }
    public DateTime SessionDate { get; set; }
    public string? SoftresId { get; set; }
    public DateTime CreatedAt { get; set; }

    public RaidWeek RaidWeek { get; set; } = null!;
    public ICollection<SoftReserve> SoftReserves { get; set; } = [];
    public ICollection<LootAward> LootAwards { get; set; } = [];
    public ICollection<SessionReservationResult> ReservationResults { get; set; } = [];
    public ICollection<UploadedFile> UploadedFiles { get; set; } = [];
}
