using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Services.PlusOne;

public sealed class PlusOneCalculator : IPlusOneCalculator
{
    public PlusOneCalculationResult Calculate(IReadOnlyList<PlusOneSessionInput> sessions)
    {
        var balances = new Dictionary<(int PlayerId, int ItemId), int>();
        var sessionRows = new List<SessionPlusOneRow>();

        var weekGroups = sessions
            .GroupBy(s => s.RaidWeekId)
            .OrderBy(g => g.Min(s => s.RaidWeekPeriodStart))
            .ThenBy(g => g.Key);

        foreach (var weekGroup in weekGroups)
        {
            var softresGroups = weekGroup.GroupBy(s => s.SoftresId ?? $"__session_{s.RaidSessionId}");

            foreach (var softresGroup in softresGroups.OrderBy(g => g.Min(s => s.SessionDate)).ThenBy(g => g.Min(s => s.RaidSessionId)))
            {
                var orderedSessions = softresGroup
                    .OrderBy(s => s.SessionDate)
                    .ThenBy(s => s.RaidSessionId)
                    .ToList();

                ProcessSoftresGroup(orderedSessions, balances, sessionRows);
            }
        }

        return new PlusOneCalculationResult
        {
            SessionRows = sessionRows,
            Balances = balances
        };
    }

    private static void ProcessSoftresGroup(
        IReadOnlyList<PlusOneSessionInput> sessions,
        Dictionary<(int PlayerId, int ItemId), int> balances,
        List<SessionPlusOneRow> sessionRows)
    {
        var evaluated = new HashSet<(int PlayerId, int ItemId)>();
        var resolved = new Dictionary<(int PlayerId, int ItemId), SessionPlusOneRow>();
        var lastSession = sessions[^1];

        foreach (var session in sessions)
        {
            var droppedItems = session.Loot
                .GroupBy(l => l.ItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var reservation in session.Reservations)
            {
                var key = (reservation.PlayerId, reservation.ItemId);

                if (evaluated.Contains(key))
                {
                    sessionRows.Add(CreateFollowUpRow(resolved[key], session.RaidSessionId));
                    continue;
                }

                var droppedThisSession = droppedItems.TryGetValue(reservation.ItemId, out var awards);
                var isLastSession = session.RaidSessionId == lastSession.RaidSessionId;

                SessionPlusOneRow row;
                if (droppedThisSession)
                {
                    row = EvaluateDrop(awards!, reservation, session.RaidSessionId, balances);
                    evaluated.Add(key);
                    resolved[key] = row;
                }
                else if (isLastSession)
                {
                    row = EvaluateDidNotDrop(reservation, session.RaidSessionId, balances);
                    evaluated.Add(key);
                    resolved[key] = row;
                }
                else
                {
                    row = new SessionPlusOneRow
                    {
                        RaidSessionId = session.RaidSessionId,
                        PlayerId = reservation.PlayerId,
                        ItemId = reservation.ItemId,
                        ItemDropped = false,
                        PlayerReceived = false,
                        PlusOneDelta = 0,
                        Reason = PlusOneReason.DidNotDrop,
                        AwardedToPlayerId = null
                    };
                }

                sessionRows.Add(row);
            }
        }
    }

    private static SessionPlusOneRow EvaluateDrop(
        IReadOnlyList<PlusOneLootInput> awards,
        PlusOneReservationInput reservation,
        int raidSessionId,
        Dictionary<(int PlayerId, int ItemId), int> balances)
    {
        var key = (reservation.PlayerId, reservation.ItemId);
        var winner = awards.FirstOrDefault(a => a.WinnerPlayerId.HasValue && !a.IsDisenchanted);
        var awardedToPlayerId = winner?.WinnerPlayerId;
        var received = winner?.WinnerPlayerId == reservation.PlayerId;

        if (received)
        {
            balances[key] = 0;
            return new SessionPlusOneRow
            {
                RaidSessionId = raidSessionId,
                PlayerId = reservation.PlayerId,
                ItemId = reservation.ItemId,
                ItemDropped = true,
                PlayerReceived = true,
                PlusOneDelta = 0,
                Reason = PlusOneReason.ReceivedItem,
                AwardedToPlayerId = awardedToPlayerId
            };
        }

        balances[key] = balances.GetValueOrDefault(key) + 1;
        return new SessionPlusOneRow
        {
            RaidSessionId = raidSessionId,
            PlayerId = reservation.PlayerId,
            ItemId = reservation.ItemId,
            ItemDropped = true,
            PlayerReceived = false,
            PlusOneDelta = 1,
            Reason = PlusOneReason.LostToOtherPlayer,
            AwardedToPlayerId = awardedToPlayerId
        };
    }

    private static SessionPlusOneRow EvaluateDidNotDrop(
        PlusOneReservationInput reservation,
        int raidSessionId,
        Dictionary<(int PlayerId, int ItemId), int> balances)
    {
        var key = (reservation.PlayerId, reservation.ItemId);
        balances[key] = balances.GetValueOrDefault(key) + 1;

        return new SessionPlusOneRow
        {
            RaidSessionId = raidSessionId,
            PlayerId = reservation.PlayerId,
            ItemId = reservation.ItemId,
            ItemDropped = false,
            PlayerReceived = false,
            PlusOneDelta = 1,
            Reason = PlusOneReason.DidNotDrop,
            AwardedToPlayerId = null
        };
    }

    private static SessionPlusOneRow CreateFollowUpRow(SessionPlusOneRow prior, int raidSessionId) =>
        new()
        {
            RaidSessionId = raidSessionId,
            PlayerId = prior.PlayerId,
            ItemId = prior.ItemId,
            ItemDropped = prior.ItemDropped,
            PlayerReceived = prior.PlayerReceived,
            PlusOneDelta = 0,
            Reason = prior.Reason,
            AwardedToPlayerId = prior.AwardedToPlayerId
        };
}
