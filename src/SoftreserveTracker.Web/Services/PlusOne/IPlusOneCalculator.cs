using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Services.PlusOne;

public sealed class PlusOneCalculationResult
{
    public required List<SessionPlusOneRow> SessionRows { get; init; }
    public required Dictionary<(int PlayerId, int ItemId), int> Balances { get; init; }
}

public sealed class SessionPlusOneRow
{
    public int RaidSessionId { get; init; }
    public int PlayerId { get; init; }
    public int ItemId { get; init; }
    public bool ItemDropped { get; init; }
    public bool PlayerReceived { get; init; }
    public int PlusOneDelta { get; init; }
    public PlusOneReason Reason { get; init; }
    public int? AwardedToPlayerId { get; init; }
}

public interface IPlusOneCalculator
{
    PlusOneCalculationResult Calculate(IReadOnlyList<PlusOneSessionInput> sessions);
}

public sealed class PlusOneSessionInput
{
    public int RaidSessionId { get; init; }
    public int RaidWeekId { get; init; }
    public DateTime RaidWeekPeriodStart { get; init; }
    public RaidType RaidType { get; init; }
    public DateTime SessionDate { get; init; }
    public required List<PlusOneReservationInput> Reservations { get; init; }
    public required List<PlusOneLootInput> Loot { get; init; }
}

public sealed class PlusOneReservationInput
{
    public int PlayerId { get; init; }
    public int ItemId { get; init; }
}

public sealed class PlusOneLootInput
{
    public int ItemId { get; init; }
    public int? WinnerPlayerId { get; init; }
    public bool IsDisenchanted { get; init; }
}
