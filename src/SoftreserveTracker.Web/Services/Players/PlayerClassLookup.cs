using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;

namespace SoftreserveTracker.Web.Services.Players;

public sealed class PlayerClassLookup(AppDbContext db) : IPlayerClassLookup
{
    public async Task<IReadOnlyDictionary<int, PlayerClassInfo>> GetLatestByPlayerIdsAsync(
        IEnumerable<int> playerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = playerIds.Distinct().ToHashSet();
        if (ids.Count == 0)
        {
            return new Dictionary<int, PlayerClassInfo>();
        }

        var softresRows = await db.SoftReserves
            .AsNoTracking()
            .Where(r => ids.Contains(r.PlayerId) && r.PlayerClass != null && r.PlayerClass != "")
            .Select(r => new { r.PlayerId, r.PlayerClass, r.Spec, r.ReservedAt })
            .ToListAsync(cancellationToken);

        var result = softresRows
            .GroupBy(r => r.PlayerId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(r => r.ReservedAt).First();
                    return new PlayerClassInfo(latest.PlayerClass!, latest.Spec);
                });

        var missing = ids.Except(result.Keys).ToList();
        if (missing.Count == 0)
        {
            return result;
        }

        var rollRows = await db.LootRolls
            .AsNoTracking()
            .Where(r => r.PlayerId.HasValue && missing.Contains(r.PlayerId.Value) && r.PlayerClass != null && r.PlayerClass != "")
            .Select(r => new { PlayerId = r.PlayerId!.Value, r.PlayerClass, r.RolledAt })
            .ToListAsync(cancellationToken);

        foreach (var group in rollRows.GroupBy(r => r.PlayerId))
        {
            var latest = group.OrderByDescending(r => r.RolledAt).First();
            result[group.Key] = new PlayerClassInfo(latest.PlayerClass!, null);
        }

        return result;
    }
}
