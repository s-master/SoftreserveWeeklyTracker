using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Infrastructure;
using SoftreserveTracker.Web.Models.ViewModels;
using SoftreserveTracker.Web.Services.Players;

namespace SoftreserveTracker.Web.Controllers;

[RosterAccess]
[Route("r/{token:guid}/sessions")]
public class SessionsController(AppDbContext db, IPlayerClassLookup playerClassLookup) : RosterControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var session = await db.RaidSessions
            .Include(s => s.RaidWeek)
            .Include(s => s.ReservationResults).ThenInclude(r => r.Player)
            .Include(s => s.ReservationResults).ThenInclude(r => r.AwardedToPlayer)
            .Include(s => s.SoftReserves)
            .Include(s => s.UploadedFiles)
            .FirstOrDefaultAsync(s => s.Id == id && s.RaidWeek.RosterId == CurrentRoster.Id, cancellationToken);

        if (session == null)
        {
            return NotFound();
        }

        var balances = await db.PlusOneBalances
            .Where(b => b.Player.RosterId == CurrentRoster.Id)
            .ToDictionaryAsync(b => (b.PlayerId, b.ItemId), b => b.CurrentCount, cancellationToken);

        var reservedAtLookup = session.SoftReserves
            .ToDictionary(r => (r.PlayerId, r.ItemId), r => (DateTime?)r.ReservedAt);

        var softresClassByKey = session.SoftReserves
            .Where(r => !string.IsNullOrWhiteSpace(r.PlayerClass))
            .ToDictionary(
                r => (r.PlayerId, r.ItemId),
                r => new PlayerClassInfo(r.PlayerClass!, r.Spec));

        var playerIds = session.ReservationResults
            .SelectMany(r => new int?[] { r.PlayerId, r.AwardedToPlayerId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var latestClasses = await playerClassLookup.GetLatestByPlayerIdsAsync(playerIds, cancellationToken);

        var rows = session.ReservationResults
            .OrderBy(r => r.Player.Name)
            .ThenBy(r => r.ItemId)
            .Select(r =>
            {
                var playerClass = PlayerDisplayBuilder.ResolveForRow(
                    r.PlayerId,
                    r.ItemId,
                    softresClassByKey,
                    latestClasses);

                return new ReservationRowViewModel
                {
                    Player = PlayerDisplayBuilder.Create(r.Player.Name, r.PlayerId, playerClass),
                    ItemId = r.ItemId,
                    ReservedAt = reservedAtLookup.GetValueOrDefault((r.PlayerId, r.ItemId)),
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
        ViewBag.Session = session;
        return View(rows);
    }
}
