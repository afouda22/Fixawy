namespace Fixawyy.Services;

public sealed class AttachmentOptions
{
    public const string SectionName = "B2B:Attachments";

    public string StoragePath { get; set; } = "App_Data/Attachments";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
    public string[] AllowedContentTypes { get; set; } = new[]
    {
        "image/jpeg",
        "image/png",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
}
