using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Services.Storage;

public interface IFileArchiveService
{
    Task SaveAsync(int raidSessionId, string originalFileName, string content, UploadFileType fileType, CancellationToken cancellationToken = default);
    Task<(string Content, string OriginalFileName)?> TryReadAsync(int uploadedFileId, CancellationToken cancellationToken = default);
    Task DeleteArchivesForSessionsAsync(IEnumerable<int> raidSessionIds, CancellationToken cancellationToken = default);
    string GetArchiveDirectory(int raidSessionId);
}
