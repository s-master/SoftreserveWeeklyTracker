namespace SoftreserveTracker.Web.Infrastructure;

/// <summary>
/// Wowhead tooltip links for TBC Anniversary.
/// Important: <c>domain=de</c> means German <em>Retail</em> tooltips.
/// Use <c>de.tbc</c> / <c>tbc</c> so tooltips.js selects dataEnv TBC (Phase 2 stats).
/// </summary>
public static class WowheadTbcLinks
{
    public static string GetHost(string? twoLetterCulture)
        => IsGerman(twoLetterCulture) ? "de.wowhead.com" : "www.wowhead.com";

    public static string GetTooltipDomain(string? twoLetterCulture)
        => IsGerman(twoLetterCulture) ? "de.tbc" : "tbc";

    public static string ItemHref(int itemId, string? twoLetterCulture)
        => $"https://{GetHost(twoLetterCulture)}/tbc/item={itemId}";

    public static string ItemDataWowhead(int itemId, string? twoLetterCulture)
        => $"item={itemId}&domain={GetTooltipDomain(twoLetterCulture)}";

    public static string ClassHref(string classSlug, string? twoLetterCulture)
        => $"https://{GetHost(twoLetterCulture)}/tbc/class={classSlug}";

    public static string ClassDataWowhead(int classId, string? twoLetterCulture)
        => $"class={classId}&domain={GetTooltipDomain(twoLetterCulture)}";

    private static bool IsGerman(string? culture)
        => string.Equals(culture, "de", StringComparison.OrdinalIgnoreCase);
}
