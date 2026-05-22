namespace SoftreserveTracker.Web.Services.Import;

public enum UploadFileKind
{
    Unknown,
    SoftresCsv,
    GargulJson
}

public sealed record ClassifiedUploadFile(string FileName, string Content, UploadFileKind Kind);
