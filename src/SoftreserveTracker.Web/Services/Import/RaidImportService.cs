using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Models.Entities;
using SoftreserveTracker.Web.Models.Enums;
using SoftreserveTracker.Web.Services.Items;
using SoftreserveTracker.Web.Services.Parsing;
using SoftreserveTracker.Web.Services.Players;
using SoftreserveTracker.Web.Services.PlusOne;
using SoftreserveTracker.Web.Services.RaidDetection;
using SoftreserveTracker.Web.Services.RaidWindow;
using SoftreserveTracker.Web.Services.Storage;

namespace SoftreserveTracker.Web.Services.Import;

public sealed class RaidImportService(
    AppDbContext db,
    ISoftresCsvParser softresParser,
    IGargulJsonParser gargulParser,
    IFileArchiveService fileArchive,
    IPlusOneCalculator plusOneCalculator,
    IUploadFileClassifier uploadFileClassifier,
    IKnownItemService knownItemService) : IRaidImportService
{
    public async Task<ImportResult> ImportAsync(
        Guid rosterId,
        string softresCsvFileName,
        Stream softresCsvStream,
        string gargulJsonFileName,
        Stream gargulJsonStream,
        CancellationToken cancellationToken = default)
    {
        var csvContent = await new StreamReader(softresCsvStream).ReadToEndAsync(cancellationToken);
        var jsonContent = await new StreamReader(gargulJsonStream).ReadToEndAsync(cancellationToken);
        return await ImportPairAsync(
            rosterId,
            softresCsvFileName,
            csvContent,
            gargulJsonFileName,
            jsonContent,
            recalculateAfter: true,
            cancellationToken);
    }

    public async Task<BulkImportResult> ImportBulkAsync(
        Guid rosterId,
        IReadOnlyList<(string FileName, string Content)> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
        {
            throw new InvalidOperationException("No files provided.");
        }

        var warnings = new List<string>();
        var classified = new List<ClassifiedUploadFile>();

        foreach (var (fileName, content) in files)
        {
            var kind = uploadFileClassifier.DetectKind(fileName, content);
            if (kind == UploadFileKind.Unknown)
            {
                throw new InvalidOperationException($"Unrecognized file type: '{fileName}'. Expected Softres CSV or Gargul JSON.");
            }

            classified.Add(new ClassifiedUploadFile(fileName, content, kind));
        }

        var pairs = uploadFileClassifier.PairFiles(classified, warnings);
        if (pairs.Count == 0)
        {
            throw new InvalidOperationException("No import pairs found. Upload at least one Softres CSV and one Gargul export.");
        }

        var allSessionIds = new List<int>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var (csv, json) in pairs)
            {
                var result = await ImportPairAsync(
                    rosterId,
                    csv.FileName,
                    csv.Content,
                    json.FileName,
                    json.Content,
                    recalculateAfter: false,
                    cancellationToken);

                allSessionIds.AddRange(result.CreatedSessionIds);
                warnings.AddRange(result.Warnings);
            }

            await RecalculatePlusOneAsync(rosterId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new BulkImportResult
        {
            RaidNightsImported = pairs.Count,
            CreatedSessionIds = allSessionIds,
            Warnings = warnings
        };
    }

    private async Task<ImportResult> ImportPairAsync(
        Guid rosterId,
        string softresCsvFileName,
        string csvContent,
        string gargulJsonFileName,
        string jsonContent,
        bool recalculateAfter,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var softres = softresParser.Parse(csvContent);
        var gargul = gargulParser.Parse(jsonContent);

        await knownItemService.UpsertAsync(
            softres.Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.ItemName))
                .Select(r => (r.ItemId, r.ItemName))
                .Concat(gargul.GroupsBySoftresId.Values
                    .SelectMany(entries => entries)
                    .Select(e => (e.ItemId, WoWItemLinkParser.ExtractItemName(e.ItemLink) ?? string.Empty))
                    .Where(e => !string.IsNullOrWhiteSpace(e.Item2))),
            cancellationToken);

        if (softres.SessionDate != gargul.SessionDate)
        {
            warnings.Add($"Session dates differ: Softres={softres.SessionDate:yyyy-MM-dd}, Gargul={gargul.SessionDate:yyyy-MM-dd}. Using Softres date.");
        }

        var sessionDate = softres.SessionDate;
        var referenceDateTime = softres.Rows.Min(r => r.ReservedAt);
        var raidWeek = await GetOrCreateRaidWeekAsync(rosterId, referenceDateTime, cancellationToken);

        var createdSessionIds = new List<int>();
        var softresTargetKey = ResolveSoftresTargetGroup(gargul, softres, warnings);
        var gargulJsonArchived = false;

        foreach (var (groupKey, lootEntries) in gargul.GroupsBySoftresId)
        {
            var groupDate = lootEntries.Min(e => e.AwardedAt).Date;
            if (groupDate != sessionDate)
            {
                var label = string.IsNullOrWhiteSpace(groupKey) ? "(none)" : $"'{groupKey}'";
                warnings.Add(
                    $"Gargul group {label} skipped: loot date {groupDate:yyyy-MM-dd} differs from Softres session {sessionDate:yyyy-MM-dd}.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(groupKey))
            {
                var raidTypeHint = ItemRaidCatalog.DetectFromItemIds(lootEntries.Select(l => l.ItemId));
                warnings.Add(
                    $"Gargul group without softresID skipped: {lootEntries.Count} loot entries on {groupDate:yyyy-MM-dd} ({raidTypeHint}).");
                continue;
            }

            var raidType = ItemRaidCatalog.DetectFromItemIds(lootEntries.Select(l => l.ItemId));
            var attachSoftres = softresTargetKey != null && groupKey == softresTargetKey;
            var sessionSoftresId = groupKey;

            var sessionExists = await db.RaidSessions
                .AsNoTracking()
                .AnyAsync(
                    s => s.RaidWeek.RosterId == rosterId
                         && s.SessionDate == sessionDate
                         && s.RaidType == raidType
                         && s.SoftresId == sessionSoftresId,
                    cancellationToken);

            if (sessionExists)
            {
                warnings.Add(
                    $"Skipped duplicate: {sessionDate:yyyy-MM-dd} {raidType} (softresID={sessionSoftresId ?? "-"}). Clear imports via Debug if re-import is intended.");
                continue;
            }

            var session = new RaidSession
            {
                RaidWeekId = raidWeek.Id,
                RaidType = raidType,
                SessionDate = sessionDate,
                SoftresId = sessionSoftresId,
                CreatedAt = DateTime.UtcNow
            };

            db.RaidSessions.Add(session);
            await db.SaveChangesAsync(cancellationToken);

            if (attachSoftres)
            {
                await AddSoftresRowsAsync(session.Id, softres.Rows, rosterId, cancellationToken);
                await fileArchive.SaveAsync(session.Id, softresCsvFileName, csvContent, UploadFileType.SoftresCsv, cancellationToken);
            }
            else
            {
                var carried = await TryAttachCarriedSoftresAsync(
                    rosterId,
                    session.Id,
                    groupKey,
                    sessionDate,
                    raidType,
                    warnings,
                    cancellationToken);

                if (!carried)
                {
                    if (raidType != softres.RaidType)
                    {
                        warnings.Add($"Gargul group '{groupKey}' detected as {raidType}; softres CSV is {softres.RaidType}. Loot imported without softres for this group.");
                    }
                    else if (softresTargetKey != null)
                    {
                        warnings.Add($"Gargul group '{groupKey}' ({raidType}): loot only; softres CSV attached to '{softresTargetKey}'.");
                    }
                }
            }

            foreach (var entry in lootEntries)
            {
                Player? winner = null;
                if (!string.IsNullOrWhiteSpace(entry.PlayerName))
                {
                    winner = await GetOrCreatePlayerAsync(rosterId, entry.PlayerName, cancellationToken);
                }

                var award = new LootAward
                {
                    RaidSessionId = session.Id,
                    ItemId = entry.ItemId,
                    WinnerPlayerId = winner?.Id,
                    AwardedToRaw = entry.AwardedToRaw,
                    SoftReserveWin = entry.SoftReserveWin,
                    IsDisenchanted = entry.IsDisenchanted,
                    AwardedAt = entry.AwardedAt,
                    SoftresId = entry.SoftresId
                };

                foreach (var roll in entry.Rolls)
                {
                    var roller = await GetOrCreatePlayerAsync(rosterId, roll.PlayerName, cancellationToken);
                    award.Rolls.Add(new LootRoll
                    {
                        PlayerId = roller.Id,
                        PlayerName = roll.PlayerName,
                        RollAmount = roll.RollAmount,
                        PlayerClass = roll.PlayerClass,
                        Classification = roll.Classification,
                        Priority = roll.Priority,
                        PlusOneState = roll.PlusOneState,
                        RolledAt = roll.RolledAt
                    });
                }

                db.LootAwards.Add(award);
            }

            if (!gargulJsonArchived)
            {
                await fileArchive.SaveAsync(session.Id, gargulJsonFileName, jsonContent, UploadFileType.GargulJson, cancellationToken);
                gargulJsonArchived = true;
            }

            await db.SaveChangesAsync(cancellationToken);
            createdSessionIds.Add(session.Id);
        }

        if (recalculateAfter)
        {
            await RecalculatePlusOneAsync(rosterId, cancellationToken);
        }

        return new ImportResult
        {
            CreatedSessionIds = createdSessionIds,
            Warnings = warnings
        };
    }

    private async Task<RaidWeek> GetOrCreateRaidWeekAsync(Guid rosterId, DateTime referenceDateTime, CancellationToken cancellationToken)
    {
        var window = RaidWindowCalculator.GetWindowForDateTime(referenceDateTime);
        var existing = await db.RaidWeeks
            .FirstOrDefaultAsync(w => w.RosterId == rosterId && w.PeriodStart == window.PeriodStart, cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        var maxWeek = await db.RaidWeeks
            .Where(w => w.RosterId == rosterId)
            .Select(w => (int?)w.WeekNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var week = new RaidWeek
        {
            RosterId = rosterId,
            WeekNumber = maxWeek + 1,
            PeriodStart = window.PeriodStart,
            PeriodEnd = window.PeriodEnd
        };

        db.RaidWeeks.Add(week);
        await db.SaveChangesAsync(cancellationToken);
        return week;
    }

    private async Task<Player> GetOrCreatePlayerAsync(Guid rosterId, string name, CancellationToken cancellationToken)
    {
        var normalized = PlayerNameNormalizer.Normalize(name);
        var player = await db.Players
            .FirstOrDefaultAsync(p => p.RosterId == rosterId && p.NormalizedName == normalized, cancellationToken);

        if (player != null)
        {
            return player;
        }

        player = new Player
        {
            RosterId = rosterId,
            Name = name.Trim(),
            NormalizedName = normalized
        };
        db.Players.Add(player);
        await db.SaveChangesAsync(cancellationToken);
        return player;
    }

    private async Task RecalculatePlusOneAsync(Guid rosterId, CancellationToken cancellationToken)
    {
        var sessions = await db.RaidSessions
            .Include(s => s.RaidWeek)
            .Include(s => s.SoftReserves)
            .Include(s => s.LootAwards)
            .Where(s => s.RaidWeek.RosterId == rosterId)
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var sessionInputs = sessions.Select(s => new PlusOneSessionInput
        {
            RaidSessionId = s.Id,
            SessionDate = s.SessionDate,
            Reservations = s.SoftReserves
                .GroupBy(r => (r.PlayerId, r.ItemId))
                .Select(g => new PlusOneReservationInput
                {
                    PlayerId = g.Key.PlayerId,
                    ItemId = g.Key.ItemId
                }).ToList(),
            Loot = s.LootAwards.Select(l => new PlusOneLootInput
            {
                ItemId = l.ItemId,
                WinnerPlayerId = l.WinnerPlayerId,
                IsDisenchanted = l.IsDisenchanted
            }).ToList()
        }).ToList();

        var result = plusOneCalculator.Calculate(sessionInputs);

        var existingResults = await db.SessionReservationResults
            .Where(r => sessions.Select(s => s.Id).Contains(r.RaidSessionId))
            .ToListAsync(cancellationToken);
        db.SessionReservationResults.RemoveRange(existingResults);

        foreach (var row in result.SessionRows)
        {
            db.SessionReservationResults.Add(new SessionReservationResult
            {
                RaidSessionId = row.RaidSessionId,
                PlayerId = row.PlayerId,
                ItemId = row.ItemId,
                ItemDropped = row.ItemDropped,
                PlayerReceived = row.PlayerReceived,
                PlusOneDelta = row.PlusOneDelta,
                Reason = row.Reason,
                AwardedToPlayerId = row.AwardedToPlayerId
            });
        }

        var playerIds = await db.Players.Where(p => p.RosterId == rosterId).Select(p => p.Id).ToListAsync(cancellationToken);
        var balances = await db.PlusOneBalances.Where(b => playerIds.Contains(b.PlayerId)).ToListAsync(cancellationToken);
        db.PlusOneBalances.RemoveRange(balances);

        foreach (var ((playerId, itemId), count) in result.Balances.Where(b => b.Value > 0))
        {
            db.PlusOneBalances.Add(new PlusOneBalance
            {
                PlayerId = playerId,
                ItemId = itemId,
                CurrentCount = count
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task AddSoftresRowsAsync(
        int sessionId,
        IEnumerable<SoftresRow> rows,
        Guid rosterId,
        CancellationToken cancellationToken)
    {
        var addedSoftresKeys = new HashSet<(int PlayerId, int ItemId)>();
        foreach (var row in rows)
        {
            var player = await GetOrCreatePlayerAsync(rosterId, row.PlayerName, cancellationToken);
            if (!addedSoftresKeys.Add((player.Id, row.ItemId)))
            {
                continue;
            }

            db.SoftReserves.Add(new SoftReserve
            {
                RaidSessionId = sessionId,
                PlayerId = player.Id,
                ItemId = row.ItemId,
                BossSource = row.BossSource,
                PlayerClass = row.PlayerClass,
                Spec = row.Spec,
                Note = row.Note,
                ReservedAt = row.ReservedAt
            });
        }
    }

    private async Task<bool> TryAttachCarriedSoftresAsync(
        Guid rosterId,
        int sessionId,
        string softresId,
        DateTime sessionDate,
        RaidType raidType,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var sourceSession = await db.RaidSessions
            .AsNoTracking()
            .Include(s => s.SoftReserves)
            .Where(s => s.RaidWeek.RosterId == rosterId
                        && s.SoftresId == softresId
                        && s.SessionDate < sessionDate
                        && s.SoftReserves.Any())
            .OrderBy(s => s.SessionDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (sourceSession == null)
        {
            return false;
        }

        if (sourceSession.RaidType != raidType)
        {
            warnings.Add(
                $"Gargul group '{softresId}' ({raidType}): no matching earlier softres session (prior session is {sourceSession.RaidType}).");
            return false;
        }

        var receivedPairs = await db.LootAwards
            .AsNoTracking()
            .Where(l => l.RaidSession.RaidWeek.RosterId == rosterId
                        && l.WinnerPlayerId != null
                        && !l.IsDisenchanted
                        && l.RaidSession.SessionDate < sessionDate)
            .Select(l => new { l.WinnerPlayerId, l.ItemId })
            .Distinct()
            .ToListAsync(cancellationToken);

        var receivedSet = receivedPairs
            .Select(p => (p.WinnerPlayerId!.Value, p.ItemId))
            .ToHashSet();

        var addedSoftresKeys = new HashSet<(int PlayerId, int ItemId)>();
        var added = 0;
        var skippedReceived = 0;

        foreach (var row in sourceSession.SoftReserves)
        {
            if (receivedSet.Contains((row.PlayerId, row.ItemId)))
            {
                skippedReceived++;
                continue;
            }

            if (!addedSoftresKeys.Add((row.PlayerId, row.ItemId)))
            {
                continue;
            }

            db.SoftReserves.Add(new SoftReserve
            {
                RaidSessionId = sessionId,
                PlayerId = row.PlayerId,
                ItemId = row.ItemId,
                BossSource = row.BossSource,
                PlayerClass = row.PlayerClass,
                Spec = row.Spec,
                Note = row.Note,
                ReservedAt = row.ReservedAt
            });
            added++;
        }

        if (added == 0)
        {
            return false;
        }

        warnings.Add(
            $"Gargul group '{softresId}': continued softres from {sourceSession.SessionDate:yyyy-MM-dd} ({added} active, {skippedReceived} already received skipped).");
        return true;
    }

    private static string? ResolveSoftresTargetGroup(
        GargulParseResult gargul,
        SoftresParseResult softres,
        List<string> warnings)
    {
        var candidates = gargul.GroupsBySoftresId
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new
            {
                Key = g.Key,
                LootCount = g.Value.Count,
                RaidType = ItemRaidCatalog.DetectFromItemIds(g.Value.Select(l => l.ItemId))
            })
            .Where(g => g.RaidType == softres.RaidType)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0].Key;
        }

        var best = candidates.OrderByDescending(c => c.LootCount).ThenBy(c => c.Key, StringComparer.Ordinal).First();
        warnings.Add(
            $"Multiple Gargul softresID groups match CSV raid type {softres.RaidType}. Softres CSV attached to '{best.Key}' ({best.LootCount} loot entries).");
        return best.Key;
    }
}
