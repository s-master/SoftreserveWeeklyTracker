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

        var reservedAtLookup = SoftReserveLookups.ReservedAtByPlayerItem(session.SoftReserves);
        var softresClassByKey = SoftReserveLookups.ClassByPlayerItem(session.SoftReserves);
        var softresNoteByKey = SoftReserveLookups.NoteBySessionPlayerItem(session.SoftReserves);

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
                    CurrentPlusOne = balances.GetValueOrDefault((r.PlayerId, r.ItemId)),
                    Note = softresNoteByKey.TryGetValue((session.Id, r.PlayerId, r.ItemId), out var note) ? note : null
                };
            }).ToList();

        ViewBag.Token = AccessToken;
        ViewBag.Session = session;
        return View(rows);
    }

    [HttpGet("{id:int}/loot")]
    public async Task<IActionResult> LootLog(int id, CancellationToken cancellationToken)
    {
        var session = await db.RaidSessions
            .AsNoTracking()
            .Include(s => s.RaidWeek)
            .FirstOrDefaultAsync(s => s.Id == id && s.RaidWeek.RosterId == CurrentRoster.Id, cancellationToken);

        if (session == null)
        {
            return NotFound();
        }

        var awards = await db.LootAwards
            .AsNoTracking()
            .Include(l => l.WinnerPlayer)
            .Include(l => l.Rolls)
            .Where(l => l.RaidSessionId == id)
            .OrderBy(l => l.AwardedAt)
            .ToListAsync(cancellationToken);

        var playerIds = awards
            .Where(a => a.WinnerPlayerId.HasValue)
            .Select(a => a.WinnerPlayerId!.Value)
            .Concat(awards.SelectMany(a => a.Rolls).Where(r => r.PlayerId.HasValue).Select(r => r.PlayerId!.Value))
            .Distinct()
            .ToList();

        var classLookup = playerIds.Count == 0
            ? new Dictionary<int, PlayerClassInfo>()
            : new Dictionary<int, PlayerClassInfo>(
                await playerClassLookup.GetLatestByPlayerIdsAsync(playerIds, cancellationToken));

        PlayerDisplayViewModel CreatePlayerDisplay(int? playerId, string? fallbackName = null)
        {
            if (playerId.HasValue)
            {
                var info = classLookup.GetValueOrDefault(playerId.Value);
                var name = awards
                    .Select(a => a.WinnerPlayer)
                    .FirstOrDefault(p => p?.Id == playerId)?.Name
                    ?? awards.SelectMany(a => a.Rolls).FirstOrDefault(r => r.PlayerId == playerId)?.PlayerName
                    ?? fallbackName
                    ?? playerId.Value.ToString();
                return PlayerDisplayBuilder.Create(name, playerId, info);
            }

            return PlayerDisplayBuilder.Create(fallbackName ?? "?", null, null);
        }

        var model = new SessionLootLogViewModel
        {
            SessionId = session.Id,
            SessionDate = session.SessionDate,
            RaidType = session.RaidType.ToString(),
            SoftresId = session.SoftresId,
            WeekNumber = session.RaidWeek.WeekNumber,
            Drops = awards.Select(a =>
            {
                var winnerId = a.IsDisenchanted ? null : a.WinnerPlayerId;
                return new SessionLootDropViewModel
                {
                    ItemId = a.ItemId,
                    LootAwardId = a.Id,
                    AwardedAt = a.AwardedAt,
                    Winner = a.IsDisenchanted
                        ? PlayerDisplayBuilder.Create("|de|", null, null)
                        : winnerId.HasValue
                            ? CreatePlayerDisplay(winnerId)
                            : null,
                    SoftReserveWin = a.SoftReserveWin,
                    IsDisenchanted = a.IsDisenchanted,
                    Rolls = a.Rolls
                        .OrderByDescending(r => r.RollAmount)
                        .ThenBy(r => r.PlayerName)
                        .Select(r => new ItemDropRollRowViewModel
                        {
                            Player = r.PlayerId.HasValue
                                ? CreatePlayerDisplay(r.PlayerId, r.PlayerName)
                                : PlayerDisplayBuilder.Create(r.PlayerName, null, null),
                            RollAmount = r.RollAmount,
                            Classification = r.Classification,
                            Priority = r.Priority,
                            RolledAt = r.RolledAt,
                            IsWinner = !a.IsDisenchanted && r.PlayerId == winnerId
                        }).ToList()
                };
            }).ToList()
        };

        ViewBag.Token = AccessToken;
        return View(model);
    }
}
