using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Services.Import;

public sealed class ImportResult
{
    public required List<int> CreatedSessionIds { get; init; }
    public required List<string> Warnings { get; init; }
}

public sealed class BulkImportResult
{
    public required int RaidNightsImported { get; init; }
    public required List<int> CreatedSessionIds { get; init; }
    public required List<string> Warnings { get; init; }
}

public interface IRaidImportService
{
    Task<ImportResult> ImportAsync(
        Guid rosterId,
        string softresCsvFileName,
        Stream softresCsvStream,
        string gargulJsonFileName,
        Stream gargulJsonStream,
        CancellationToken cancellationToken = default);

    Task<BulkImportResult> ImportBulkAsync(
        Guid rosterId,
        IReadOnlyList<(string FileName, string Content)> files,
        CancellationToken cancellationToken = default);
}
