using SoftreserveTracker.Web.Models.Entities;
using SoftreserveTracker.Web.Services.Players;

namespace SoftreserveTracker.Web.Infrastructure;

public static class SoftReserveLookups
{
    public static Dictionary<(int PlayerId, int ItemId), DateTime?> ReservedAtByPlayerItem(
        IEnumerable<SoftReserve> rows) =>
        rows.GroupBy(r => (r.PlayerId, r.ItemId))
            .ToDictionary(g => g.Key, g => (DateTime?)g.Max(r => r.ReservedAt));

    public static Dictionary<(int RaidSessionId, int PlayerId, int ItemId), DateTime?> ReservedAtBySessionPlayerItem(
        IEnumerable<SoftReserve> rows) =>
        rows.GroupBy(r => (r.RaidSessionId, r.PlayerId, r.ItemId))
            .ToDictionary(g => g.Key, g => (DateTime?)g.Max(r => r.ReservedAt));

    public static Dictionary<(int RaidSessionId, int ItemId), DateTime?> ReservedAtBySessionItem(
        IEnumerable<(int RaidSessionId, int ItemId, DateTime ReservedAt)> rows) =>
        rows.GroupBy(r => (r.RaidSessionId, r.ItemId))
            .ToDictionary(g => g.Key, g => (DateTime?)g.Max(r => r.ReservedAt));

    public static Dictionary<(int PlayerId, int ItemId), PlayerClassInfo> ClassByPlayerItem(
        IEnumerable<SoftReserve> rows) =>
        rows.Where(r => !string.IsNullOrWhiteSpace(r.PlayerClass))
            .GroupBy(r => (r.PlayerId, r.ItemId))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(r => r.ReservedAt).First();
                    return new PlayerClassInfo(latest.PlayerClass!, latest.Spec);
                });

    public static Dictionary<(int RaidSessionId, int PlayerId, int ItemId), PlayerClassInfo> ClassBySessionPlayerItem(
        IEnumerable<SoftReserve> rows) =>
        rows.Where(r => !string.IsNullOrWhiteSpace(r.PlayerClass))
            .GroupBy(r => (r.RaidSessionId, r.PlayerId, r.ItemId))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(r => r.ReservedAt).First();
                    return new PlayerClassInfo(latest.PlayerClass!, latest.Spec);
                });

    public static Dictionary<(int RaidSessionId, int PlayerId, int ItemId), string?> NoteBySessionPlayerItem(
        IEnumerable<SoftReserve> rows) =>
        rows.Where(r => !string.IsNullOrWhiteSpace(r.Note))
            .GroupBy(r => (r.RaidSessionId, r.PlayerId, r.ItemId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.ReservedAt).First().Note);
}
