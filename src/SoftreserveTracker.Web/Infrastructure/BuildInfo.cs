using System.Reflection;

namespace SoftreserveTracker.Web.Infrastructure;

public static class BuildInfo
{
    private static readonly Assembly Assembly = typeof(BuildInfo).Assembly;

    public static string Number =>
        Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetName().Version?.ToString(3)
        ?? "dev";

    public static DateTime BuiltAtLocal
    {
        get
        {
            var location = Assembly.Location;
            if (string.IsNullOrEmpty(location) || !File.Exists(location))
            {
                return DateTime.UtcNow.ToLocalTime();
            }

            return File.GetLastWriteTimeUtc(location).ToLocalTime();
        }
    }
}
