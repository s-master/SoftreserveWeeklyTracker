using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SoftreserveTracker.Web.Infrastructure;
using SoftreserveTracker.Web.Resources;
using SoftreserveTracker.Web.Services.Debug;

namespace SoftreserveTracker.Web.Controllers;

[DebugEnabled]
[Route("debug")]
public class DebugController(
    IDebugAdminService debugAdmin,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var rosters = await debugAdmin.GetRosterSummariesAsync(cancellationToken);
        return View(rosters);
    }

    [HttpPost("rosters/{rosterId:guid}/clear-imports")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearImports(Guid rosterId, CancellationToken cancellationToken)
    {
        await debugAdmin.ClearImportDataAsync(rosterId, cancellationToken);
        TempData["Success"] = localizer["Debug_ImportsCleared"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("rosters/{rosterId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoster(Guid rosterId, CancellationToken cancellationToken)
    {
        await debugAdmin.DeleteRosterAsync(rosterId, cancellationToken);
        TempData["Success"] = localizer["Debug_RosterDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }
}
