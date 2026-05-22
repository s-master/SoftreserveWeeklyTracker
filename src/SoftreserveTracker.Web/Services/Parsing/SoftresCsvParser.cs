using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using SoftreserveTracker.Web.Services.RaidDetection;

namespace SoftreserveTracker.Web.Services.Parsing;

public sealed class SoftresCsvParser : ISoftresCsvParser
{
    public SoftresParseResult Parse(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<SoftresCsvRecord>().ToList();

        if (records.Count == 0)
        {
            throw new InvalidOperationException("Softres CSV contains no data rows.");
        }

        var rows = records.Select(r => new SoftresRow
        {
            ItemId = r.ItemId,
            ItemName = r.Item?.Trim() ?? string.Empty,
            BossSource = r.From?.Trim(),
            PlayerName = r.Name?.Trim() ?? string.Empty,
            PlayerClass = r.Class?.Trim(),
            Spec = r.Spec?.Trim(),
            Note = r.Note?.Trim(),
            ReservedAt = ParseDate(r.Date)
        }).ToList();

        var sessionDate = rows.Min(r => r.ReservedAt).Date;
        var raidType = RaidTypeDetector.DetectFromBosses(rows.Select(r => r.BossSource));

        return new SoftresParseResult
        {
            Rows = rows,
            SessionDate = sessionDate,
            RaidType = raidType
        };
    }

    private static DateTime ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Softres CSV row is missing a date.");
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
        {
            return dt;
        }

        throw new InvalidOperationException($"Unable to parse softres date: {value}");
    }

    private sealed class SoftresCsvRecord
    {
        public string? Item { get; set; }
        public int ItemId { get; set; }
        public string? From { get; set; }
        public string? Name { get; set; }
        public string? Class { get; set; }
        public string? Spec { get; set; }
        public string? Note { get; set; }
        public string? Date { get; set; }
    }
}
