using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Services.Storage;

namespace SoftreserveTracker.Web.Services.Debug;

public sealed class DebugAdminService(AppDbContext db, IFileArchiveService fileArchive) : IDebugAdminService
{
    public async Task<IReadOnlyList<DebugRosterSummary>> GetRosterSummariesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Rosters
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new DebugRosterSummary
            {
                Id = r.Id,
                Name = r.Name,
                AccessToken = r.AccessToken,
                CreatedAt = r.CreatedAt,
                WeekCount = r.RaidWeeks.Count,
                SessionCount = r.RaidWeeks.SelectMany(w => w.Sessions).Count(),
                PlayerCount = r.Players.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task ClearImportDataAsync(Guid rosterId, CancellationToken cancellationToken = default)
    {
        var roster = await db.Rosters.FirstOrDefaultAsync(r => r.Id == rosterId, cancellationToken)
            ?? throw new InvalidOperationException("Roster not found.");

        var sessionIds = await db.RaidSessions
            .Where(s => s.RaidWeek.RosterId == rosterId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var playerIds = await db.Players
            .Where(p => p.RosterId == rosterId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (sessionIds.Count > 0)
        {
            var results = await db.SessionReservationResults
                .Where(r => sessionIds.Contains(r.RaidSessionId))
                .ToListAsync(cancellationToken);
            db.SessionReservationResults.RemoveRange(results);

            var softres = await db.SoftReserves
                .Where(r => sessionIds.Contains(r.RaidSessionId))
                .ToListAsync(cancellationToken);
            db.SoftReserves.RemoveRange(softres);

            var loot = await db.LootAwards
                .Where(l => sessionIds.Contains(l.RaidSessionId))
                .ToListAsync(cancellationToken);
            var awardIds = loot.Select(l => l.Id).ToList();

            if (awardIds.Count > 0)
            {
                var rolls = await db.LootRolls
                    .Where(r => awardIds.Contains(r.LootAwardId))
                    .ToListAsync(cancellationToken);
                db.LootRolls.RemoveRange(rolls);
            }

            db.LootAwards.RemoveRange(loot);

            var files = await db.UploadedFiles
                .Where(f => sessionIds.Contains(f.RaidSessionId))
                .ToListAsync(cancellationToken);
            db.UploadedFiles.RemoveRange(files);

            var sessions = await db.RaidSessions
                .Where(s => sessionIds.Contains(s.Id))
                .ToListAsync(cancellationToken);
            db.RaidSessions.RemoveRange(sessions);
        }

        var weeks = await db.RaidWeeks.Where(w => w.RosterId == rosterId).ToListAsync(cancellationToken);
        db.RaidWeeks.RemoveRange(weeks);

        if (playerIds.Count > 0)
        {
            var balances = await db.PlusOneBalances
                .Where(b => playerIds.Contains(b.PlayerId))
                .ToListAsync(cancellationToken);
            db.PlusOneBalances.RemoveRange(balances);

            var players = await db.Players.Where(p => p.RosterId == rosterId).ToListAsync(cancellationToken);
            db.Players.RemoveRange(players);
        }

        await db.SaveChangesAsync(cancellationToken);
        await fileArchive.DeleteArchivesForSessionsAsync(sessionIds, cancellationToken);
    }

    public async Task DeleteRosterAsync(Guid rosterId, CancellationToken cancellationToken = default)
    {
        await ClearImportDataAsync(rosterId, cancellationToken);

        var roster = await db.Rosters.FirstOrDefaultAsync(r => r.Id == rosterId, cancellationToken)
            ?? throw new InvalidOperationException("Roster not found.");

        db.Rosters.Remove(roster);
        await db.SaveChangesAsync(cancellationToken);
    }
}
