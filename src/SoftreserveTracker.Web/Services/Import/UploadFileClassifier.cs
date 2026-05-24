using System.Text.Json;
using SoftreserveTracker.Web.Services.Parsing;

namespace SoftreserveTracker.Web.Services.Import;

public interface IUploadFileClassifier
{
    UploadFileKind DetectKind(string fileName, string content);
    DateTime GetSessionDate(UploadFileKind kind, string content);
    IReadOnlyList<(ClassifiedUploadFile Csv, ClassifiedUploadFile Json)> PairFiles(
        IReadOnlyList<ClassifiedUploadFile> files,
        IList<string> warnings);
}

public sealed class UploadFileClassifier(
    ISoftresCsvParser softresParser,
    IGargulJsonParser gargulParser) : IUploadFileClassifier
{
    public UploadFileKind DetectKind(string fileName, string content)
    {
        if (LooksLikeGargulJson(content))
        {
            return UploadFileKind.GargulJson;
        }

        if (LooksLikeSoftresCsv(fileName, content))
        {
            return UploadFileKind.SoftresCsv;
        }

        return UploadFileKind.Unknown;
    }

    public DateTime GetSessionDate(UploadFileKind kind, string content) =>
        kind switch
        {
            UploadFileKind.SoftresCsv => softresParser.Parse(content).SessionDate,
            UploadFileKind.GargulJson => gargulParser.Parse(content).SessionDate,
            _ => throw new InvalidOperationException("Cannot determine session date for unknown file type.")
        };

    public IReadOnlyList<(ClassifiedUploadFile Csv, ClassifiedUploadFile Json)> PairFiles(
        IReadOnlyList<ClassifiedUploadFile> files,
        IList<string> warnings)
    {
        var csvFiles = files.Where(f => f.Kind == UploadFileKind.SoftresCsv).ToList();
        var jsonFiles = files.Where(f => f.Kind == UploadFileKind.GargulJson).ToList();
        var pairs = new List<(ClassifiedUploadFile Csv, ClassifiedUploadFile Json)>();
        var usedJson = new HashSet<ClassifiedUploadFile>();

        foreach (var csv in csvFiles.OrderBy(c => GetSessionDate(UploadFileKind.SoftresCsv, c.Content)))
        {
            var csvDate = GetSessionDate(UploadFileKind.SoftresCsv, csv.Content);
            var candidates = jsonFiles
                .Where(j => !usedJson.Contains(j) && gargulParser.GetSessionDates(j.Content).Contains(csvDate))
                .ToList();

            if (candidates.Count == 0)
            {
                var gargulDates = jsonFiles
                    .SelectMany(j => gargulParser.GetSessionDates(j.Content))
                    .Distinct()
                    .OrderBy(d => d)
                    .Select(d => d.ToString("yyyy-MM-dd"))
                    .ToList();

                var hint = gargulDates.Count > 0
                    ? $" Uploaded Gargul export(s) contain loot from: {string.Join(", ", gargulDates)}."
                    : string.Empty;

                throw new InvalidOperationException(
                    $"No Gargul export found for Softres file '{csv.FileName}' (session date {csvDate:yyyy-MM-dd}).{hint}");
            }

            var match = candidates[0];
            if (candidates.Count > 1)
            {
                warnings.Add(
                    $"Multiple Gargul files for {csvDate:yyyy-MM-dd}; paired '{csv.FileName}' with '{match.FileName}'.");
            }

            usedJson.Add(match);
            pairs.Add((csv, match));
        }

        foreach (var orphan in jsonFiles.Where(j => !usedJson.Contains(j)))
        {
            var date = GetSessionDate(UploadFileKind.GargulJson, orphan.Content);
            throw new InvalidOperationException(
                $"No Softres CSV found for Gargul file '{orphan.FileName}' (session date {date:yyyy-MM-dd}).");
        }

        return pairs;
    }

    private static bool LooksLikeGargulJson(string content)
    {
        ReadOnlySpan<char> trimmed = content.AsSpan().TrimStart();
        if (trimmed.Length == 0 || (trimmed[0] != '[' && trimmed[0] != '{'))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return false;
            }

            var first = doc.RootElement[0];
            return first.TryGetProperty("itemID", out _) || first.TryGetProperty("itemId", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool LooksLikeSoftresCsv(string fileName, string content)
    {
        var firstNewline = content.IndexOf('\n');
        var headerLine = (firstNewline >= 0 ? content[..firstNewline] : content).Trim();
        if (headerLine.Length == 0)
        {
            return false;
        }

        var hasItemId = headerLine.Contains("ItemId", StringComparison.OrdinalIgnoreCase);
        var hasName = headerLine.Contains("Name", StringComparison.OrdinalIgnoreCase);
        var hasItem = headerLine.Contains("Item", StringComparison.OrdinalIgnoreCase);

        if (hasItemId && (hasName || hasItem))
        {
            return true;
        }

        return fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
               && headerLine.Contains(',', StringComparison.Ordinal);
    }
}
