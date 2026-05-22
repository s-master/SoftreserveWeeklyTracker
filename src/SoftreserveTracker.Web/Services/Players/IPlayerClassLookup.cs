namespace SoftreserveTracker.Web.Services.Players;

public interface IPlayerClassLookup
{
    Task<IReadOnlyDictionary<int, PlayerClassInfo>> GetLatestByPlayerIdsAsync(
        IEnumerable<int> playerIds,
        CancellationToken cancellationToken = default);
}
