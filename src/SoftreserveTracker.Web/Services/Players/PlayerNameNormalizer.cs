namespace SoftreserveTracker.Web.Services.Players;

public static class PlayerNameNormalizer
{
    public static string Normalize(string name) => name.Trim().ToLowerInvariant();
}
