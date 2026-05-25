using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Infrastructure;
using SoftreserveTracker.Web.Models.ViewModels;
using SoftreserveTracker.Web.Resources;
using SoftreserveTracker.Web.Services.Items;
using SoftreserveTracker.Web.Services.Players;

namespace SoftreserveTracker.Web.Controllers;

[RosterAccess]
[Route("r/{token:guid}/overview")]
public class OverviewController(
    AppDbContext db,
    IPlayerClassLookup playerClassLookup,
    IKnownItemService knownItemService,
    IStringLocalizer<SharedResource> localizer) : RosterControllerBase
{
    [HttpGet("players")]
    public async Task<IActionResult> Players(CancellationToken cancellationToken)
    {
        var rows = await BuildPlayerOverviewAsync(cancellationToken);
        ViewBag.Token = AccessToken;

        var allPlayerIds = await db.Players
            .AsNoTracking()
            .Where(p => p.RosterId == CurrentRoster.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var classLookup = await playerClassLookup.GetLatestByPlayerIdsAsync(allPlayerIds, cancellationToken);

        var allPlayers = await db.Players
            .AsNoTracking()
            .Where(p => p.RosterId == CurrentRoster.Id)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        ViewBag.AllPlayers = allPlayers.Select(p => new PlayerLinkViewModel
        {
            Id = p.Id,
            Name = p.Name,
            PlayerClass = classLookup.GetValueOrDefault(p.Id)?.PlayerClass,
            Spec = classLookup.GetValueOrDefault(p.Id)?.Spec
        }).ToList();

        var playersWithPlusOne = rows.Select(r => r.Player.PlayerId).Distinct().Count();
        var entryCount = rows.Count;
        var totalPlusOne = rows.Sum(r => r.CurrentPlusOne);
        SetOpenGraph(
            localizer["Og_Players_Title", CurrentRoster.Name].Value,
            localizer["Og_Players_Description", playersWithPlusOne, entryCount, totalPlusOne].Value);

        return View(rows);
    }

    [HttpGet("players/{playerId:int}")]
    public async Task<IActionResult> Player(int playerId, CancellationToken cancellationToken)
    {
        var player = await db.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId && p.RosterId == CurrentRoster.Id, cancellationToken);

        if (player == null)
        {
            return NotFound();
        }

        ViewBag.Token = AccessToken;
        var detail = await BuildPlayerDetailAsync(player.Id, player.Name, cancellationToken);
        SetOpenGraph(
            localizer["Og_Player_Title", player.Name, CurrentRoster.Name].Value,
            localizer["Og_Player_Description", player.Name, detail.ActivePlusOnes.Count, detail.LootWon.Count].Value);
        return View(detail);
    }

    [HttpGet("items")]
    public async Task<IActionResult> Items(CancellationToken cancellationToken)
    {
        var rows = await BuildItemOverviewAsync(cancellationToken);
        ViewBag.Token = AccessToken;

        var itemCount = rows.Select(r => r.ItemId).Distinct().Count();
        var entryCount = rows.Count;
        var playerCount = rows.Select(r => r.Player.PlayerId).Distinct().Count();
        SetOpenGraph(
            localizer["Og_Items_Title", CurrentRoster.Name].Value,
            localizer["Og_Items_Description", itemCount, playerCount, entryCount].Value);

        return View(rows);
    }

    [HttpGet("items/{itemId:int}")]
    public async Task<IActionResult> Item(int itemId, CancellationToken cancellationToken)
    {
        var detail = await BuildItemDetailAsync(itemId, cancellationToken);
        if (detail == null)
        {
            return NotFound();
        }

        ViewBag.Token = AccessToken;
        SetOpenGraph(
            localizer["Og_ItemDetail_Title", detail.ItemName ?? detail.ItemId.ToString(), CurrentRoster.Name].Value,
            localizer["Og_ItemDetail_Description", detail.ActivePlusOnes.Count, detail.DropEvents.Count, detail.SoftReserves.Count].Value);
        return View(detail);
    }

    private async Task<List<PlayerOverviewRowViewModel>> BuildPlayerOverviewAsync(CancellationToken cancellationToken)
    {
        var balances = await db.PlusOneBalances
            .AsNoTracking()
            .Include(b => b.Player)
            .Where(b => b.Player.RosterId == CurrentRoster.Id && b.CurrentCount > 0)
            .OrderBy(b => b.Player.Name)
            .ThenBy(b => b.ItemId)
            .ToListAsync(cancellationToken);

        if (balances.Count == 0)
        {
            return [];
        }

        var playerIds = balances.Select(b => b.PlayerId).Distinct().ToList();
        var itemIds = balances.Select(b => b.ItemId).Distinct().ToList();
        var classLookup = await playerClassLookup.GetLatestByPlayerIdsAsync(playerIds, cancellationToken);

        var lastReserved = await db.SoftReserves
            .AsNoTracking()
            .Where(r => playerIds.Contains(r.PlayerId) && itemIds.Contains(r.ItemId))
            .GroupBy(r => new { r.PlayerId, r.ItemId })
            .Select(g => new { g.Key.PlayerId, g.Key.ItemId, LastAt = g.Max(r => r.ReservedAt) })
            .ToListAsync(cancellationToken);

        var lastLookup = lastReserved.ToDictionary(x => (x.PlayerId, x.ItemId), x => (DateTime?)x.LastAt);

        return balances.Select(b => new PlayerOverviewRowViewModel
        {
            Player = PlayerDisplayBuilder.Create(
                b.Player.Name,
                b.PlayerId,
                classLookup.GetValueOrDefault(b.PlayerId)),
            ItemId = b.ItemId,
            CurrentPlusOne = b.CurrentCount,
            LastReservedAt = lastLookup.GetValueOrDefault((b.PlayerId, b.ItemId))
        }).ToList();
    }

    private async Task<PlayerDetailViewModel> BuildPlayerDetailAsync(
        int playerId,
        string playerName,
        CancellationToken cancellationToken)
    {
        var rosterId = CurrentRoster.Id;
        var playerClassLookupResult = await playerClassLookup.GetLatestByPlayerIdsAsync([playerId], cancellationToken);
        var playerClassInfo = playerClassLookupResult.GetValueOrDefault(playerId);

        var balances = await db.PlusOneBalances
            .AsNoTracking()
            .Where(b => b.PlayerId == playerId && b.CurrentCount > 0)
            .OrderByDescending(b => b.CurrentCount)
            .ThenBy(b => b.ItemId)
            .ToListAsync(cancellationToken);

        var balanceItemIds = balances.Select(b => b.ItemId).ToList();
        var lastReserved = balanceItemIds.Count == 0
            ? []
            : await db.SoftReserves
                .AsNoTracking()
                .Where(r => r.PlayerId == playerId && balanceItemIds.Contains(r.ItemId))
                .GroupBy(r => r.ItemId)
                .Select(g => new { g.Key, LastAt = g.Max(r => r.ReservedAt) })
                .ToListAsync(cancellationToken);

        var lastLookup = lastReserved.ToDictionary(x => x.Key, x => (DateTime?)x.LastAt);

        var allBalances = await db.PlusOneBalances
            .AsNoTracking()
            .Where(b => b.PlayerId == playerId)
            .ToDictionaryAsync(b => b.ItemId, b => b.CurrentCount, cancellationToken);

        var results = await db.SessionReservationResults
            .AsNoTracking()
            .Include(r => r.RaidSession).ThenInclude(s => s.RaidWeek)
            .Include(r => r.AwardedToPlayer)
            .Where(r => r.PlayerId == playerId && r.Player.RosterId == rosterId)
            .OrderByDescending(r => r.RaidSession.SessionDate)
            .ThenBy(r => r.ItemId)
            .ToListAsync(cancellationToken);

        var sessionIds = results.Select(r => r.RaidSessionId).Distinct().ToList();
        var softresRows = sessionIds.Count == 0
            ? []
            : await db.SoftReserves
                .AsNoTracking()
                .Where(r => r.PlayerId == playerId && sessionIds.Contains(r.RaidSessionId))
                .Select(r => new { r.RaidSessionId, r.ItemId, r.ReservedAt })
                .ToListAsync(cancellationToken);

        var softresLookup = SoftReserveLookups.ReservedAtBySessionItem(
            softresRows.Select(r => (r.RaidSessionId, r.ItemId, r.ReservedAt)));

        var awardedToIds = results
            .Where(r => r.AwardedToPlayerId.HasValue)
            .Select(r => r.AwardedToPlayerId!.Value)
            .Distinct()
            .ToList();

        var awardedToClassLookup = awardedToIds.Count == 0
            ? new Dictionary<int, PlayerClassInfo>()
            : new Dictionary<int, PlayerClassInfo>(
                await playerClassLookup.GetLatestByPlayerIdsAsync(awardedToIds, cancellationToken));

        var lootWon = await db.LootAwards
            .AsNoTracking()
            .Include(l => l.RaidSession).ThenInclude(s => s.RaidWeek)
            .Where(l => l.WinnerPlayerId == playerId && l.RaidSession.RaidWeek.RosterId == rosterId)
            .OrderByDescending(l => l.AwardedAt)
            .ToListAsync(cancellationToken);

        var rolls = await db.LootRolls
            .AsNoTracking()
            .Include(r => r.LootAward).ThenInclude(a => a.RaidSession).ThenInclude(s => s.RaidWeek)
            .Where(r => r.PlayerId == playerId && r.LootAward.RaidSession.RaidWeek.RosterId == rosterId)
            .OrderByDescending(r => r.RolledAt)
            .ToListAsync(cancellationToken);

        var rollBySessionItem = rolls
            .GroupBy(r => (r.LootAward.RaidSessionId, r.LootAward.ItemId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.RollAmount).First());

        return new PlayerDetailViewModel
        {
            PlayerId = playerId,
            PlayerName = playerName,
            PlayerClass = playerClassInfo?.PlayerClass,
            Spec = playerClassInfo?.Spec,
            ActivePlusOnes = balances.Select(b => new PlayerPlusOneRowViewModel
            {
                ItemId = b.ItemId,
                CurrentPlusOne = b.CurrentCount,
                LastReservedAt = lastLookup.GetValueOrDefault(b.ItemId)
            }).ToList(),
            ReservationHistory = results.Select(r =>
            {
                var roll = rollBySessionItem.GetValueOrDefault((r.RaidSessionId, r.ItemId));
                return new PlayerReservationHistoryRowViewModel
                {
                    SessionId = r.RaidSessionId,
                    SessionDate = r.RaidSession.SessionDate,
                    RaidType = r.RaidSession.RaidType.ToString(),
                    WeekNumber = r.RaidSession.RaidWeek.WeekNumber,
                    ItemId = r.ItemId,
                    ReservedAt = softresLookup.GetValueOrDefault((r.RaidSessionId, r.ItemId)),
                    ItemDropped = r.ItemDropped,
                    PlayerReceived = r.PlayerReceived,
                    PlusOneDelta = r.PlusOneDelta,
                    Reason = r.Reason.ToString(),
                    AwardedTo = r.AwardedToPlayer == null
                        ? null
                        : PlayerDisplayBuilder.Create(
                            r.AwardedToPlayer.Name,
                            r.AwardedToPlayerId,
                            awardedToClassLookup.GetValueOrDefault(r.AwardedToPlayerId!.Value)),
                    CurrentPlusOne = allBalances.GetValueOrDefault(r.ItemId),
                    RollAmount = roll?.RollAmount,
                    RollClassification = roll?.Classification
                };
            }).ToList(),
            LootWon = lootWon.Select(l => new PlayerLootWonRowViewModel
            {
                SessionDate = l.RaidSession.SessionDate,
                RaidType = l.RaidSession.RaidType.ToString(),
                ItemId = l.ItemId,
                AwardedAt = l.AwardedAt,
                SoftReserveWin = l.SoftReserveWin,
                IsDisenchanted = l.IsDisenchanted
            }).ToList(),
            Rolls = rolls.Select(r => new PlayerRollRowViewModel
            {
                SessionDate = r.LootAward.RaidSession.SessionDate,
                RaidType = r.LootAward.RaidSession.RaidType.ToString(),
                ItemId = r.LootAward.ItemId,
                RollAmount = r.RollAmount,
                Classification = r.Classification
            }).ToList()
        };
    }

    private async Task<List<ItemOverviewRowViewModel>> BuildItemOverviewAsync(CancellationToken cancellationToken)
    {
        var rosterId = CurrentRoster.Id;

        var receivedPairs = await db.LootAwards
            .AsNoTracking()
            .Include(l => l.RaidSession).ThenInclude(s => s.RaidWeek)
            .Where(l => l.RaidSession.RaidWeek.RosterId == rosterId && l.WinnerPlayerId != null && !l.IsDisenchanted)
            .Select(l => new { l.WinnerPlayerId, l.ItemId })
            .Distinct()
            .ToListAsync(cancellationToken);

        var receivedSet = receivedPairs
            .Select(p => (p.WinnerPlayerId!.Value, p.ItemId))
            .ToHashSet();

        var balances = await db.PlusOneBalances
            .AsNoTracking()
            .Include(b => b.Player)
            .Where(b => b.Player.RosterId == rosterId)
            .ToListAsync(cancellationToken);

        if (balances.Count == 0)
        {
            return [];
        }

        var playerIds = balances.Select(b => b.PlayerId).Distinct().ToList();
        var itemIds = balances.Select(b => b.ItemId).Distinct().ToList();
        var classLookup = await playerClassLookup.GetLatestByPlayerIdsAsync(playerIds, cancellationToken);

        var lastReserved = await db.SoftReserves
            .AsNoTracking()
            .Where(r => playerIds.Contains(r.PlayerId) && itemIds.Contains(r.ItemId))
            .GroupBy(r => new { r.PlayerId, r.ItemId })
            .Select(g => new { g.Key.PlayerId, g.Key.ItemId, LastAt = g.Max(r => r.ReservedAt) })
            .ToListAsync(cancellationToken);

        var lastLookup = lastReserved.ToDictionary(x => (x.PlayerId, x.ItemId), x => (DateTime?)x.LastAt);

        var itemNames = await knownItemService.GetNamesByItemIdsAsync(itemIds, cancellationToken);
        if (itemIds.Any(id => !itemNames.ContainsKey(id)))
        {
            await knownItemService.SyncFromRosterArchivesAsync(CurrentRoster.Id, cancellationToken);
            itemNames = await knownItemService.GetNamesByItemIdsAsync(itemIds, cancellationToken);
        }

        return balances
            .OrderBy(b => b.ItemId)
            .ThenByDescending(b => b.CurrentCount)
            .Select(entry => new ItemOverviewRowViewModel
            {
                ItemId = entry.ItemId,
                ItemName = itemNames.GetValueOrDefault(entry.ItemId),
                Player = PlayerDisplayBuilder.Create(
                    entry.Player.Name,
                    entry.PlayerId,
                    classLookup.GetValueOrDefault(entry.PlayerId)),
                CurrentPlusOne = entry.CurrentCount,
                HasReceived = receivedSet.Contains((entry.PlayerId, entry.ItemId)),
                LastReservedAt = lastLookup.GetValueOrDefault((entry.PlayerId, entry.ItemId))
            }).ToList();
    }

    private async Task<ItemDetailViewModel?> BuildItemDetailAsync(int itemId, CancellationToken cancellationToken)
    {
        var rosterId = CurrentRoster.Id;

        var hasData =
            await db.PlusOneBalances
                .AsNoTracking()
                .AnyAsync(b => b.ItemId == itemId && b.Player.RosterId == rosterId, cancellationToken)
            || await db.SoftReserves
                .AsNoTracking()
                .AnyAsync(r => r.ItemId == itemId && r.RaidSession.RaidWeek.RosterId == rosterId, cancellationToken)
            || await db.LootAwards
                .AsNoTracking()
                .AnyAsync(l => l.ItemId == itemId && l.RaidSession.RaidWeek.RosterId == rosterId, cancellationToken)
            || await db.SessionReservationResults
                .AsNoTracking()
                .AnyAsync(r => r.ItemId == itemId && r.Player.RosterId == rosterId, cancellationToken);

        if (!hasData)
        {
            return null;
        }

        var itemNames = await knownItemService.GetNamesByItemIdsAsync([itemId], cancellationToken);
        if (!itemNames.ContainsKey(itemId))
        {
            await knownItemService.SyncFromRosterArchivesAsync(rosterId, cancellationToken);
            itemNames = await knownItemService.GetNamesByItemIdsAsync([itemId], cancellationToken);
        }

        var receivedPlayerIds = await db.LootAwards
            .AsNoTracking()
            .Where(l => l.ItemId == itemId
                        && l.RaidSession.RaidWeek.RosterId == rosterId
                        && l.WinnerPlayerId != null
                        && !l.IsDisenchanted)
            .Select(l => l.WinnerPlayerId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var receivedSet = receivedPlayerIds.ToHashSet();

        var balances = await db.PlusOneBalances
            .AsNoTracking()
            .Include(b => b.Player)
            .Where(b => b.ItemId == itemId && b.Player.RosterId == rosterId && b.CurrentCount > 0)
            .OrderByDescending(b => b.CurrentCount)
            .ThenBy(b => b.Player.Name)
            .ToListAsync(cancellationToken);

        var softReserves = await db.SoftReserves
            .AsNoTracking()
            .Include(r => r.Player)
            .Include(r => r.RaidSession).ThenInclude(s => s.RaidWeek)
            .Where(r => r.ItemId == itemId && r.RaidSession.RaidWeek.RosterId == rosterId)
            .OrderByDescending(r => r.ReservedAt)
            .ToListAsync(cancellationToken);

        var dropAwards = await db.LootAwards
            .AsNoTracking()
            .Include(l => l.WinnerPlayer)
            .Include(l => l.Rolls).ThenInclude(r => r.Player)
            .Include(l => l.RaidSession).ThenInclude(s => s.RaidWeek)
            .Where(l => l.ItemId == itemId && l.RaidSession.RaidWeek.RosterId == rosterId)
            .OrderByDescending(l => l.AwardedAt)
            .ToListAsync(cancellationToken);

        var plusOneHistory = await db.SessionReservationResults
            .AsNoTracking()
            .Include(r => r.Player)
            .Include(r => r.AwardedToPlayer)
            .Include(r => r.RaidSession).ThenInclude(s => s.RaidWeek)
            .Where(r => r.ItemId == itemId && r.Player.RosterId == rosterId)
            .OrderByDescending(r => r.RaidSession.SessionDate)
            .ThenBy(r => r.Player.Name)
            .ToListAsync(cancellationToken);

        var playerIds = balances.Select(b => b.PlayerId)
            .Concat(softReserves.Select(r => r.PlayerId))
            .Concat(dropAwards.Where(l => l.WinnerPlayerId.HasValue).Select(l => l.WinnerPlayerId!.Value))
            .Concat(dropAwards.SelectMany(l => l.Rolls).Where(r => r.PlayerId.HasValue).Select(r => r.PlayerId!.Value))
            .Concat(plusOneHistory.Select(r => r.PlayerId))
            .Concat(plusOneHistory.Where(r => r.AwardedToPlayerId.HasValue).Select(r => r.AwardedToPlayerId!.Value))
            .Distinct()
            .ToList();

        var classLookup = playerIds.Count == 0
            ? new Dictionary<int, PlayerClassInfo>()
            : new Dictionary<int, PlayerClassInfo>(
                await playerClassLookup.GetLatestByPlayerIdsAsync(playerIds, cancellationToken));

        var lastReserved = softReserves
            .GroupBy(r => r.PlayerId)
            .ToDictionary(g => g.Key, g => (DateTime?)g.Max(r => r.ReservedAt));

        var softresBySessionPlayer = softReserves
            .GroupBy(r => (r.RaidSessionId, r.PlayerId))
            .ToDictionary(g => g.Key, g => (DateTime?)g.Max(r => r.ReservedAt));

        var rollBySessionPlayer = dropAwards
            .SelectMany(a => a.Rolls.Where(r => r.PlayerId.HasValue).Select(r => new
            {
                a.RaidSessionId,
                r.PlayerId,
                r.RollAmount,
                r.Classification
            }))
            .GroupBy(x => (x.RaidSessionId, x.PlayerId!.Value))
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.RollAmount).First());

        PlayerDisplayViewModel CreatePlayerDisplay(int? playerId, string? fallbackName = null)
        {
            if (playerId.HasValue)
            {
                var info = classLookup.GetValueOrDefault(playerId.Value);
                var name = softReserves.FirstOrDefault(r => r.PlayerId == playerId)?.Player.Name
                    ?? dropAwards.Select(l => l.WinnerPlayer).FirstOrDefault(p => p?.Id == playerId)?.Name
                    ?? plusOneHistory.FirstOrDefault(r => r.PlayerId == playerId)?.Player.Name
                    ?? fallbackName
                    ?? playerId.Value.ToString();
                return PlayerDisplayBuilder.Create(name, playerId, info);
            }

            return PlayerDisplayBuilder.Create(fallbackName ?? "?", null, null);
        }

        return new ItemDetailViewModel
        {
            ItemId = itemId,
            ItemName = itemNames.GetValueOrDefault(itemId),
            ActivePlusOnes = balances.Select(b => new ItemPlusOneHolderRowViewModel
            {
                Player = PlayerDisplayBuilder.Create(
                    b.Player.Name,
                    b.PlayerId,
                    classLookup.GetValueOrDefault(b.PlayerId)),
                CurrentPlusOne = b.CurrentCount,
                LastReservedAt = lastReserved.GetValueOrDefault(b.PlayerId),
                HasReceived = receivedSet.Contains(b.PlayerId)
            }).ToList(),
            SoftReserves = softReserves.Select(r => new ItemSoftReserveRowViewModel
            {
                Player = PlayerDisplayBuilder.Create(
                    r.Player.Name,
                    r.PlayerId,
                    classLookup.GetValueOrDefault(r.PlayerId)),
                SessionDate = r.RaidSession.SessionDate,
                RaidType = r.RaidSession.RaidType.ToString(),
                WeekNumber = r.RaidSession.RaidWeek.WeekNumber,
                SessionId = r.RaidSessionId,
                ReservedAt = r.ReservedAt,
                BossSource = r.BossSource
            }).ToList(),
            DropEvents = dropAwards.Select(a =>
            {
                var winnerId = a.IsDisenchanted ? null : a.WinnerPlayerId;
                return new ItemDropEventViewModel
                {
                    LootAwardId = a.Id,
                    SessionDate = a.RaidSession.SessionDate,
                    RaidType = a.RaidSession.RaidType.ToString(),
                    WeekNumber = a.RaidSession.RaidWeek.WeekNumber,
                    SessionId = a.RaidSessionId,
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
            }).ToList(),
            PlusOneHistory = plusOneHistory.Select(r =>
            {
                var roll = r.PlayerId != 0
                    ? rollBySessionPlayer.GetValueOrDefault((r.RaidSessionId, r.PlayerId))
                    : null;
                return new ItemPlusOneHistoryRowViewModel
                {
                    Player = PlayerDisplayBuilder.Create(
                        r.Player.Name,
                        r.PlayerId,
                        classLookup.GetValueOrDefault(r.PlayerId)),
                    SessionDate = r.RaidSession.SessionDate,
                    RaidType = r.RaidSession.RaidType.ToString(),
                    WeekNumber = r.RaidSession.RaidWeek.WeekNumber,
                    SessionId = r.RaidSessionId,
                    ReservedAt = softresBySessionPlayer.GetValueOrDefault((r.RaidSessionId, r.PlayerId)),
                    ItemDropped = r.ItemDropped,
                    PlayerReceived = r.PlayerReceived,
                    PlusOneDelta = r.PlusOneDelta,
                    Reason = r.Reason.ToString(),
                    AwardedTo = r.AwardedToPlayer == null
                        ? null
                        : PlayerDisplayBuilder.Create(
                            r.AwardedToPlayer.Name,
                            r.AwardedToPlayerId,
                            classLookup.GetValueOrDefault(r.AwardedToPlayerId!.Value)),
                    RollAmount = roll?.RollAmount,
                    RollClassification = roll?.Classification
                };
            }).ToList()
        };
    }
}
