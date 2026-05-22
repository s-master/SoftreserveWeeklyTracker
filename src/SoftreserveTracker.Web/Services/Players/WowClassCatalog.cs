using System.Globalization;

namespace SoftreserveTracker.Web.Services.Players;

public static class WowClassCatalog
{
    public sealed record ClassMeta(int Id, string Slug);

    private static readonly Dictionary<string, ClassMeta> ByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["warrior"] = new(1, "warrior"),
        ["paladin"] = new(2, "paladin"),
        ["hunter"] = new(3, "hunter"),
        ["rogue"] = new(4, "rogue"),
        ["priest"] = new(5, "priest"),
        ["shaman"] = new(7, "shaman"),
        ["mage"] = new(8, "mage"),
        ["warlock"] = new(9, "warlock"),
        ["druid"] = new(11, "druid"),
    };

    public static ClassMeta? Resolve(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        return ByName.GetValueOrDefault(className.Trim());
    }

    public static string DisplayClassName(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return string.Empty;
        }

        var trimmed = className.Trim();
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
    }

    public static string IconUrl(string slug) =>
        $"https://wow.zamimg.com/images/wow/icons/small/classicon_{slug}.jpg";
}
