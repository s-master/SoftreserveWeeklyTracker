using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SoftreserveTracker.Web.Models;
using SoftreserveTracker.Web.Models.ViewModels;
using SoftreserveTracker.Web.Resources;
using SoftreserveTracker.Web.Services.Rosters;

namespace SoftreserveTracker.Web.Controllers;

public class HomeController(
    IRosterService rosterService,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new CreateRosterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRosterViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), localizer["Validation_RosterNameRequired"]);
        }

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var roster = await rosterService.CreateAsync(model.Name.Trim(), cancellationToken);
        return RedirectToAction("Index", "Roster", new { token = roster.AccessToken });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
