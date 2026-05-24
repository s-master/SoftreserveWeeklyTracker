using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Infrastructure;
using SoftreserveTracker.Web.Models.ViewModels;
using SoftreserveTracker.Web.Resources;

namespace SoftreserveTracker.Web.Controllers;

[RosterAccess]
[Route("r/{token:guid}")]
public class RosterController(AppDbContext db, IStringLocalizer<SharedResource> localizer) : RosterControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var weeks = await db.RaidWeeks
            .AsNoTracking()
            .Include(w => w.Sessions)
            .Where(w => w.RosterId == CurrentRoster.Id)
            .OrderByDescending(w => w.WeekNumber)
            .ToListAsync(cancellationToken);

        var model = weeks.Select(w => new WeekSummaryViewModel
        {
            Id = w.Id,
            WeekNumber = w.WeekNumber,
            PeriodStart = w.PeriodStart,
            PeriodEnd = w.PeriodEnd,
            Sessions = w.Sessions
                .OrderBy(s => s.SessionDate)
                .Select(s => new SessionSummaryViewModel
                {
                    Id = s.Id,
                    RaidType = s.RaidType.ToString(),
                    SessionDate = s.SessionDate,
                    SoftresId = s.SoftresId,
                    WeekNumber = w.WeekNumber
                }).ToList()
        }).ToList();

        ViewBag.RosterName = CurrentRoster.Name;
        ViewBag.Token = AccessToken;

        var weekCount = weeks.Count;
        var sessionCount = weeks.Sum(w => w.Sessions.Count);
        SetOpenGraph(
            localizer["Og_Dashboard_Title", CurrentRoster.Name].Value,
            localizer["Og_Dashboard_Description", CurrentRoster.Name, weekCount, sessionCount].Value);

        return View(model);
    }
}
