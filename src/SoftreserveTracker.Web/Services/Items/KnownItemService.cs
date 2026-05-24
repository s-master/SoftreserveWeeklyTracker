using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Models.Entities;
using SoftreserveTracker.Web.Models.Enums;
using SoftreserveTracker.Web.Services.Parsing;
using SoftreserveTracker.Web.Services.Storage;

namespace SoftreserveTracker.Web.Services.Items;

public sealed class KnownItemService(
    AppDbContext db,
    IFileArchiveService fileArchive,
    ISoftresCsvParser softresParser,
    IGargulJsonParser gargulParser) : IKnownItemService
{
    public async Task UpsertAsync(IEnumerable<(int ItemId, string Name)> items, CancellationToken cancellationToken = default)
    {
        var byId = items
            .Where(i => i.ItemId > 0 && !string.IsNullOrWhiteSpace(i.Name))
            .GroupBy(i => i.ItemId)
            .ToDictionary(g => g.Key, g => g.Last().Name.Trim());

        if (byId.Count == 0)
        {
            return;
        }

        var existing = await db.KnownItems
            .Where(i => byId.Keys.Contains(i.ItemId))
            .ToDictionaryAsync(i => i.ItemId, cancellationToken);

        foreach (var (itemId, name) in byId)
        {
            if (existing.TryGetValue(itemId, out var row))
            {
                if (!string.Equals(row.Name, name, StringComparison.Ordinal))
                {
                    row.Name = name;
                }

                continue;
            }

            db.KnownItems.Add(new KnownItem
            {
                ItemId = itemId,
                Name = name
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetNamesByItemIdsAsync(
        IEnumerable<int> itemIds,
        CancellationToken cancellationToken = default)
    {
        var ids = itemIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        return await db.KnownItems
            .AsNoTracking()
            .Where(i => ids.Contains(i.ItemId))
            .ToDictionaryAsync(i => i.ItemId, i => i.Name, cancellationToken);
    }

    public async Task SyncFromRosterArchivesAsync(Guid rosterId, CancellationToken cancellationToken = default)
    {
        var files = await db.UploadedFiles
            .AsNoTracking()
            .Where(f => f.RaidSession.RaidWeek.RosterId == rosterId
                        && (f.FileType == UploadFileType.SoftresCsv || f.FileType == UploadFileType.GargulJson))
            .Select(f => new { f.Id, f.FileType })
            .ToListAsync(cancellationToken);

        if (files.Count == 0)
        {
            return;
        }

        var items = new List<(int ItemId, string Name)>();

        foreach (var file in files)
        {
            var payload = await fileArchive.TryReadAsync(file.Id, cancellationToken);
            if (payload == null)
            {
                continue;
            }

            if (file.FileType == UploadFileType.SoftresCsv)
            {
                var softres = softresParser.Parse(payload.Value.Content);
                items.AddRange(softres.Rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.ItemName))
                    .Select(r => (r.ItemId, r.ItemName)));
                continue;
            }

            var gargul = gargulParser.Parse(payload.Value.Content);
            items.AddRange(gargul.GroupsBySoftresId.Values
                .SelectMany(entries => entries)
                .Select(e => (e.ItemId, WoWItemLinkParser.ExtractItemName(e.ItemLink) ?? string.Empty))
                .Where(e => !string.IsNullOrWhiteSpace(e.Item2))
                .Select(e => (e.Item1, e.Item2)));
        }

        await UpsertAsync(items, cancellationToken);
    }
}
