using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Services.PlusOne;

public sealed class PlusOneCalculator : IPlusOneCalculator
{
    public PlusOneCalculationResult Calculate(IReadOnlyList<PlusOneSessionInput> sessions)
    {
        var ordered = sessions.OrderBy(s => s.SessionDate).ThenBy(s => s.RaidSessionId).ToList();
        var balances = new Dictionary<(int PlayerId, int ItemId), int>();
        var sessionRows = new List<SessionPlusOneRow>();

        foreach (var session in ordered)
        {
            var droppedItems = session.Loot
                .GroupBy(l => l.ItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var reservation in session.Reservations)
            {
                var key = (reservation.PlayerId, reservation.ItemId);
                var dropped = droppedItems.TryGetValue(reservation.ItemId, out var awards);
                var received = false;
                int? awardedToPlayerId = null;
                PlusOneReason reason;
                var delta = 0;

                if (dropped)
                {
                    var winner = awards!.FirstOrDefault(a => a.WinnerPlayerId.HasValue && !a.IsDisenchanted);
                    awardedToPlayerId = winner?.WinnerPlayerId;
                    received = winner?.WinnerPlayerId == reservation.PlayerId;

                    if (received)
                    {
                        reason = PlusOneReason.ReceivedItem;
                        balances[key] = 0;
                    }
                    else
                    {
                        reason = PlusOneReason.LostToOtherPlayer;
                        delta = 1;
                        balances[key] = balances.GetValueOrDefault(key) + 1;
                    }
                }
                else
                {
                    reason = PlusOneReason.DidNotDrop;
                    delta = 1;
                    balances[key] = balances.GetValueOrDefault(key) + 1;
                }

                sessionRows.Add(new SessionPlusOneRow
                {
                    RaidSessionId = session.RaidSessionId,
                    PlayerId = reservation.PlayerId,
                    ItemId = reservation.ItemId,
                    ItemDropped = dropped,
                    PlayerReceived = received,
                    PlusOneDelta = delta,
                    Reason = reason,
                    AwardedToPlayerId = awardedToPlayerId
                });
            }
        }

        return new PlusOneCalculationResult
        {
            SessionRows = sessionRows,
            Balances = balances
        };
    }
}
