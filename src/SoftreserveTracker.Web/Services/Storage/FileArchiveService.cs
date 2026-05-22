using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Data;
using SoftreserveTracker.Web.Models.Entities;
using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Services.Storage;

public sealed class FileArchiveService(IWebHostEnvironment env, AppDbContext db) : IFileArchiveService
{
    public string GetArchiveDirectory(int raidSessionId)
    {
        var path = Path.Combine(env.ContentRootPath, "App_Data", "archives", raidSessionId.ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    public async Task SaveAsync(int raidSessionId, string originalFileName, string content, UploadFileType fileType, CancellationToken cancellationToken = default)
    {
        var storedFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{SanitizeFileName(originalFileName)}";
        var dir = GetArchiveDirectory(raidSessionId);
        var fullPath = Path.Combine(dir, storedFileName);
        await File.WriteAllTextAsync(fullPath, content, cancellationToken);

        db.UploadedFiles.Add(new UploadedFile
        {
            RaidSessionId = raidSessionId,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            FileType = fileType,
            UploadedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(string Content, string OriginalFileName)?> TryReadAsync(int uploadedFileId, CancellationToken cancellationToken = default)
    {
        var file = await db.UploadedFiles.FindAsync([uploadedFileId], cancellationToken);
        if (file == null)
        {
            return null;
        }

        var fullPath = Path.Combine(GetArchiveDirectory(file.RaidSessionId), file.StoredFileName);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
        return (content, file.OriginalFileName);
    }

    public Task DeleteArchivesForSessionsAsync(IEnumerable<int> raidSessionIds, CancellationToken cancellationToken = default)
    {
        foreach (var raidSessionId in raidSessionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dir = Path.Combine(env.ContentRootPath, "App_Data", "archives", raidSessionId.ToString());
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }
}
