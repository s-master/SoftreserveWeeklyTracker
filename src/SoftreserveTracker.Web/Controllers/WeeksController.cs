using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Infrastructure;
using SoftreserveTracker.Web.Models.ViewModels;
using SoftreserveTracker.Web.Services.Players;

namespace SoftreserveTracker.Web.Controllers;

[RosterAccess]
[Route("r/{token:guid}/weeks")]
public class WeeksController(AppDbContext db, IPlayerClassLookup playerClassLookup) : RosterControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var week = await db.RaidWeeks
            .Include(w => w.Sessions)
            .FirstOrDefaultAsync(w => w.Id == id && w.RosterId == CurrentRoster.Id, cancellationToken);

        if (week == null)
        {
            return NotFound();
        }

        var sessionIds = week.Sessions.Select(s => s.Id).ToList();
        var results = await db.SessionReservationResults
            .Include(r => r.Player)
            .Include(r => r.AwardedToPlayer)
            .Where(r => sessionIds.Contains(r.RaidSessionId))
            .ToListAsync(cancellationToken);

        var softReserves = await db.SoftReserves
            .Where(r => sessionIds.Contains(r.RaidSessionId))
            .ToListAsync(cancellationToken);

        var reservedAtLookup = SoftReserveLookups.ReservedAtBySessionPlayerItem(softReserves);
        var softresClassByKey = SoftReserveLookups.ClassBySessionPlayerItem(softReserves);

        var balances = await db.PlusOneBalances
            .Where(b => b.Player.RosterId == CurrentRoster.Id)
            .ToDictionaryAsync(b => (b.PlayerId, b.ItemId), b => b.CurrentCount, cancellationToken);

        var playerIds = results
            .SelectMany(r => new int?[] { r.PlayerId, r.AwardedToPlayerId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var latestClasses = await playerClassLookup.GetLatestByPlayerIdsAsync(playerIds, cancellationToken);

        var rows = results
            .OrderBy(r => r.RaidSessionId)
            .ThenBy(r => r.Player.Name)
            .Select(r =>
            {
                var playerClass = PlayerDisplayBuilder.ResolveForWeekRow(
                    r.RaidSessionId,
                    r.PlayerId,
                    r.ItemId,
                    softresClassByKey,
                    latestClasses);

                return new ReservationRowViewModel
                {
                    Player = PlayerDisplayBuilder.Create(r.Player.Name, r.PlayerId, playerClass),
                    ItemId = r.ItemId,
                    ReservedAt = reservedAtLookup.GetValueOrDefault((r.RaidSessionId, r.PlayerId, r.ItemId)),
                    PlusOneDelta = r.PlusOneDelta,
                    PlayerReceived = r.PlayerReceived,
                    ItemDropped = r.ItemDropped,
                    Reason = r.Reason.ToString(),
                    AwardedTo = r.AwardedToPlayer == null
                        ? null
                        : PlayerDisplayBuilder.Create(
                            r.AwardedToPlayer.Name,
                            r.AwardedToPlayerId,
                            latestClasses.GetValueOrDefault(r.AwardedToPlayerId!.Value)),
                    CurrentPlusOne = balances.GetValueOrDefault((r.PlayerId, r.ItemId))
                };
            }).ToList();

        ViewBag.Token = AccessToken;
        ViewBag.Week = week;
        return View(rows);
    }
}
