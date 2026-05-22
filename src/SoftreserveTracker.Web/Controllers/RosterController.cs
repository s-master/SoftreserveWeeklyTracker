using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Infrastructure;
using SoftreserveTracker.Web.Models.ViewModels;

namespace SoftreserveTracker.Web.Controllers;

[RosterAccess]
[Route("r/{token:guid}")]
public class RosterController(AppDbContext db) : RosterControllerBase
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
        return View(model);
    }
}
