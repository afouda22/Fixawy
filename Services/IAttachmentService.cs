using Microsoft.AspNetCore.Http;

namespace Fixawyy.Services;

public sealed record AttachmentDownload(string FileName, string ContentType, Stream Content);

public interface IAttachmentService
{
    Task<IReadOnlyList<int>> UploadAsync(
        int ticketId,
        string userId,
        bool canViewAll,
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken = default);

    Task<AttachmentDownload?> DownloadAsync(
        int attachmentId,
        string userId,
        bool canViewAll,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> DeleteAsync(
        int attachmentId,
        string userId,
        bool canViewAll,
        CancellationToken cancellationToken = default);
}
