namespace SoftreserveTracker.Web.Services.Debug;

public sealed class DebugRosterSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid AccessToken { get; init; }
    public DateTime CreatedAt { get; init; }
    public int WeekCount { get; init; }
    public int SessionCount { get; init; }
    public int PlayerCount { get; init; }
}

public interface IDebugAdminService
{
    Task<IReadOnlyList<DebugRosterSummary>> GetRosterSummariesAsync(CancellationToken cancellationToken = default);
    Task ClearImportDataAsync(Guid rosterId, CancellationToken cancellationToken = default);
    Task DeleteRosterAsync(Guid rosterId, CancellationToken cancellationToken = default);
}
