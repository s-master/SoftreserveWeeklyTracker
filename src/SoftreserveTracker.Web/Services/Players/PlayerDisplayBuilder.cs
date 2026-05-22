using SoftreserveTracker.Web.Models.ViewModels;

namespace SoftreserveTracker.Web.Services.Players;

public static class PlayerDisplayBuilder
{
    public static PlayerDisplayViewModel Create(
        string name,
        int? playerId = null,
        PlayerClassInfo? classInfo = null,
        string? linkUrl = null,
        bool hideName = false)
    {
        return new PlayerDisplayViewModel
        {
            Name = name,
            PlayerId = playerId,
            PlayerClass = classInfo?.PlayerClass,
            Spec = classInfo?.Spec,
            LinkUrl = linkUrl,
            HideName = hideName
        };
    }

    public static PlayerDisplayViewModel? CreateOptional(
        string? name,
        int? playerId,
        IReadOnlyDictionary<int, PlayerClassInfo> lookup,
        string? linkUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var classInfo = playerId.HasValue ? lookup.GetValueOrDefault(playerId.Value) : null;
        return Create(name, playerId, classInfo, linkUrl);
    }

    public static PlayerClassInfo? ResolveForRow(
        int playerId,
        int itemId,
        IReadOnlyDictionary<(int PlayerId, int ItemId), PlayerClassInfo> rowSpecific,
        IReadOnlyDictionary<int, PlayerClassInfo> latest)
    {
        if (rowSpecific.TryGetValue((playerId, itemId), out var rowInfo))
        {
            return rowInfo;
        }

        return latest.GetValueOrDefault(playerId);
    }

    public static PlayerClassInfo? ResolveForWeekRow(
        int sessionId,
        int playerId,
        int itemId,
        IReadOnlyDictionary<(int SessionId, int PlayerId, int ItemId), PlayerClassInfo> rowSpecific,
        IReadOnlyDictionary<int, PlayerClassInfo> latest)
    {
        if (rowSpecific.TryGetValue((sessionId, playerId, itemId), out var rowInfo))
        {
            return rowInfo;
        }

        return latest.GetValueOrDefault(playerId);
    }
}
