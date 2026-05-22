using SoftreserveTracker.Web.Models.Enums;

namespace SoftreserveTracker.Web.Models.Entities;

public class UploadedFile
{
    public int Id { get; set; }
    public int RaidSessionId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public UploadFileType FileType { get; set; }
    public DateTime UploadedAt { get; set; }

    public RaidSession RaidSession { get; set; } = null!;
}
