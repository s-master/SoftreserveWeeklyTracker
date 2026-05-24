namespace SoftreserveTracker.Web.Models.ViewModels;

using System.ComponentModel.DataAnnotations;

public sealed class CreateRosterViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [Display(Name = "Home_RosterName")]
    public string Name { get; set; } = string.Empty;
}

public sealed class UploadRaidViewModel
{
    public List<IFormFile> Files { get; set; } = [];
}

public sealed class ReservationRowViewModel
{
    public PlayerDisplayViewModel Player { get; set; } = new();
    public int ItemId { get; set; }
    public DateTime? ReservedAt { get; set; }
    public int PlusOneDelta { get; set; }
    public bool PlayerReceived { get; set; }
    public bool ItemDropped { get; set; }
    public string Reason { get; set; } = string.Empty;
    public PlayerDisplayViewModel? AwardedTo { get; set; }
    public int CurrentPlusOne { get; set; }
}

public sealed class ItemOverviewRowViewModel
{
    public int ItemId { get; set; }
    public string? ItemName { get; set; }
    public PlayerDisplayViewModel Player { get; set; } = new();
    public int CurrentPlusOne { get; set; }
    public bool HasReceived { get; set; }
    public DateTime? LastReservedAt { get; set; }
}

public sealed class PlayerLinkViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PlayerClass { get; set; }
    public string? Spec { get; set; }
}

public sealed class PlayerOverviewRowViewModel
{
    public PlayerDisplayViewModel Player { get; set; } = new();
    public int ItemId { get; set; }
    public int CurrentPlusOne { get; set; }
    public DateTime? LastReservedAt { get; set; }
}

public sealed class PlayerDisplayViewModel
{
    public string Name { get; set; } = string.Empty;
    public int? PlayerId { get; set; }
    public string? PlayerClass { get; set; }
    public string? Spec { get; set; }
    public string? LinkUrl { get; set; }
    public bool HideName { get; set; }
    public bool LinkName { get; set; } = true;
    public bool LinkClassIcon { get; set; } = true;
}

public sealed class PlayerDetailViewModel
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string? PlayerClass { get; set; }
    public string? Spec { get; set; }
    public List<PlayerPlusOneRowViewModel> ActivePlusOnes { get; set; } = [];
    public List<PlayerReservationHistoryRowViewModel> ReservationHistory { get; set; } = [];
    public List<PlayerLootWonRowViewModel> LootWon { get; set; } = [];
    public List<PlayerRollRowViewModel> Rolls { get; set; } = [];
}

public sealed class PlayerPlusOneRowViewModel
{
    public int ItemId { get; set; }
    public int CurrentPlusOne { get; set; }
    public DateTime? LastReservedAt { get; set; }
}

public sealed class PlayerReservationHistoryRowViewModel
{
    public DateTime SessionDate { get; set; }
    public string RaidType { get; set; } = string.Empty;
    public int WeekNumber { get; set; }
    public int ItemId { get; set; }
    public DateTime? ReservedAt { get; set; }
    public bool ItemDropped { get; set; }
    public bool PlayerReceived { get; set; }
    public int PlusOneDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public PlayerDisplayViewModel? AwardedTo { get; set; }
    public int CurrentPlusOne { get; set; }
    public int SessionId { get; set; }
    public int? RollAmount { get; set; }
    public string? RollClassification { get; set; }
}

public sealed class PlayerLootWonRowViewModel
{
    public DateTime SessionDate { get; set; }
    public string RaidType { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public DateTime AwardedAt { get; set; }
    public bool SoftReserveWin { get; set; }
    public bool IsDisenchanted { get; set; }
}

public sealed class PlayerRollRowViewModel
{
    public DateTime SessionDate { get; set; }
    public string RaidType { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public int RollAmount { get; set; }
    public string? Classification { get; set; }
}

public sealed class SessionSummaryViewModel
{
    public int Id { get; set; }
    public string RaidType { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public string? SoftresId { get; set; }
    public int WeekNumber { get; set; }
}

public sealed class WeekSummaryViewModel
{
    public int Id { get; set; }
    public int WeekNumber { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<SessionSummaryViewModel> Sessions { get; set; } = [];
}

public sealed class ArchiveFileViewModel
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public int RaidSessionId { get; set; }
    public DateTime SessionDate { get; set; }
}
