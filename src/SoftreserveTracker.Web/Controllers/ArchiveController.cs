using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Infrastructure;
using SoftreserveTracker.Web.Models.ViewModels;
using SoftreserveTracker.Web.Services.Storage;

namespace SoftreserveTracker.Web.Controllers;

[RosterAccess]
[Route("r/{token:guid}/archive")]
public class ArchiveController(AppDbContext db, IFileArchiveService fileArchive) : RosterControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var files = await db.UploadedFiles
            .Include(f => f.RaidSession).ThenInclude(s => s.RaidWeek)
            .Where(f => f.RaidSession.RaidWeek.RosterId == CurrentRoster.Id)
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new ArchiveFileViewModel
            {
                Id = f.Id,
                OriginalFileName = f.OriginalFileName,
                FileType = f.FileType.ToString(),
                UploadedAt = f.UploadedAt,
                RaidSessionId = f.RaidSessionId,
                SessionDate = f.RaidSession.SessionDate
            })
            .ToListAsync(cancellationToken);

        ViewBag.Token = AccessToken;
        return View(files);
    }

    [HttpGet("download/{id:int}")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var file = await db.UploadedFiles
            .Include(f => f.RaidSession).ThenInclude(s => s.RaidWeek)
            .FirstOrDefaultAsync(f => f.Id == id && f.RaidSession.RaidWeek.RosterId == CurrentRoster.Id, cancellationToken);

        if (file == null)
        {
            return NotFound();
        }

        var content = await fileArchive.TryReadAsync(id, cancellationToken);
        if (content == null)
        {
            return NotFound();
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(content.Value.Content);
        return File(bytes, "application/octet-stream", content.Value.OriginalFileName);
    }
}
