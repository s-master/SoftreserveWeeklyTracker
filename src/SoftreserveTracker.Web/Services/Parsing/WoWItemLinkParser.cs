namespace SoftreserveTracker.Web.Services.Parsing;

public static class WoWItemLinkParser
{
    public static string? ExtractItemName(string? itemLink)
    {
        if (string.IsNullOrWhiteSpace(itemLink))
        {
            return null;
        }

        var start = itemLink.IndexOf("|h[", StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += 3;
        var end = itemLink.IndexOf("]|h", start, StringComparison.Ordinal);
        if (end < 0)
        {
            return null;
        }

        var name = itemLink[start..end].Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
