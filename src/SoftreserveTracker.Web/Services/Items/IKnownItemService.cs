namespace SoftreserveTracker.Web.Services.Items;

public interface IKnownItemService
{
    Task UpsertAsync(IEnumerable<(int ItemId, string Name)> items, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> GetNamesByItemIdsAsync(
        IEnumerable<int> itemIds,
        CancellationToken cancellationToken = default);

    Task SyncFromRosterArchivesAsync(Guid rosterId, CancellationToken cancellationToken = default);
}
