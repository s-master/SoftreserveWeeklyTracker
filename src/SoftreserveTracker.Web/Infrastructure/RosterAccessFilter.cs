using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SoftreserveTracker.Web.Models.Entities;
using SoftreserveTracker.Web.Services.Rosters;

namespace SoftreserveTracker.Web.Infrastructure;

public sealed class RosterAccessFilter(IRosterService rosterService) : IAsyncActionFilter
{
    public const string RosterKey = "CurrentRoster";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.RouteData.Values.TryGetValue("token", out var tokenValue) ||
            !Guid.TryParse(tokenValue?.ToString(), out var token))
        {
            context.Result = new NotFoundResult();
            return;
        }

        var roster = await rosterService.GetByAccessTokenAsync(token);
        if (roster == null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        context.HttpContext.Items[RosterKey] = roster;
        await next();
    }

    public static Roster GetRoster(HttpContext httpContext)
    {
        return (Roster)httpContext.Items[RosterKey]!;
    }
}

public class RosterAccessAttribute : ServiceFilterAttribute
{
    public RosterAccessAttribute() : base(typeof(RosterAccessFilter))
    {
    }
}

public class RosterControllerBase : Controller
{
    protected Roster CurrentRoster => RosterAccessFilter.GetRoster(HttpContext);
    protected Guid AccessToken => CurrentRoster.AccessToken;

    protected void SetOpenGraph(string title, string description, string? imagePath = null)
    {
        ViewData[OpenGraphKeys.Title] = title;
        ViewData[OpenGraphKeys.Description] = description;
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            ViewData[OpenGraphKeys.ImagePath] = imagePath;
        }
    }
}
