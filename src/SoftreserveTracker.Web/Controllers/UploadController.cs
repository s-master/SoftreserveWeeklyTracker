using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SoftreserveTracker.Web.Infrastructure;
using SoftreserveTracker.Web.Models.ViewModels;
using SoftreserveTracker.Web.Resources;
using SoftreserveTracker.Web.Services.Import;

namespace SoftreserveTracker.Web.Controllers;

[RosterAccess]
[Route("r/{token:guid}/upload")]
public class UploadController(
    IRaidImportService importService,
    IStringLocalizer<SharedResource> localizer) : RosterControllerBase
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewBag.Token = AccessToken;
        return View(new UploadRaidViewModel());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UploadRaidViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Token = AccessToken;

        var files = model.Files?.Where(f => f.Length > 0).ToList() ?? [];
        if (files.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Files), localizer["Validation_FilesRequired"]);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var inputs = new List<(string FileName, string Content)>();
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            inputs.Add((file.FileName, await reader.ReadToEndAsync(cancellationToken)));
        }

        try
        {
            var result = await importService.ImportBulkAsync(CurrentRoster.Id, inputs, cancellationToken);

            TempData["Success"] = (result.RaidNightsImported == 1
                ? localizer["Upload_SuccessSingle", result.CreatedSessionIds.Count]
                : localizer["Upload_SuccessBulk", result.RaidNightsImported, result.CreatedSessionIds.Count]).Value;

            if (result.Warnings.Count > 0)
            {
                TempData["Warnings"] = string.Join(" | ", result.Warnings);
            }

            return RedirectToAction("Index", "Roster", new { token = AccessToken });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
