using System.Text;
using Fixawyy.Context;
using Fixawyy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fixawyy.Services;

public sealed class AttachmentService : IAttachmentService
{
    private readonly AppDbContext _context;
    private readonly AttachmentOptions _options;
    private readonly string _storageRoot;

    public AttachmentService(
        AppDbContext context,
        IOptions<AttachmentOptions> options,
        IWebHostEnvironment environment)
    {
        _context = context;
        _options = options.Value;

        var configuredPath = _options.StoragePath.Trim();
        if (Path.IsPathRooted(configuredPath) || configuredPath.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Attachment storage path must be a relative path without traversal segments.");
        }

        _storageRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        var contentRoot = Path.GetFullPath(environment.ContentRootPath)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!_storageRoot.StartsWith(contentRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Attachment storage path must be inside the application content root.");
        }
    }

    public async Task<IReadOnlyList<int>> UploadAsync(
        int ticketId,
        string userId,
        bool canViewAll,
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        var ticketQuery = _context.MaintenanceTickets
            .Where(t => t.Id == ticketId);
        if (!canViewAll)
        {
            ticketQuery = ticketQuery.Where(t => t.SubmittedByUserId == userId);
        }

        if (!await ticketQuery.AnyAsync(cancellationToken))
        {
            throw new UnauthorizedAccessException("You are not authorized to attach files to this request.");
        }

        var uploaded = new List<TicketAttachment>();
        var physicalPaths = new List<string>();
        try
        {
            foreach (var file in files.Where(f => f != null))
            {
                var attachment = await SaveFileAsync(ticketId, file, physicalPaths, cancellationToken);
                uploaded.Add(attachment);
            }

            if (uploaded.Count == 0)
            {
                return Array.Empty<int>();
            }

            await _context.TicketAttachments.AddRangeAsync(uploaded, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return uploaded.Select(a => a.Id).ToArray();
        }
        catch
        {
            foreach (var path in physicalPaths)
            {
                DeletePhysicalFile(path);
            }

            throw;
        }
    }

    public async Task<AttachmentDownload?> DownloadAsync(
        int attachmentId,
        string userId,
        bool canViewAll,
        CancellationToken cancellationToken = default)
    {
        var attachment = await GetAuthorizedAttachmentAsync(
            attachmentId,
            userId,
            canViewAll,
            cancellationToken);

        if (attachment == null)
        {
            return null;
        }

        var path = GetSafePhysicalPath(attachment.FilePath);
        if (!System.IO.File.Exists(path))
        {
            return null;
        }

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return new AttachmentDownload(attachment.FileName, attachment.ContentType, stream);
    }

    public async Task<(bool Succeeded, string? Error)> DeleteAsync(
        int attachmentId,
        string userId,
        bool canViewAll,
        CancellationToken cancellationToken = default)
    {
        var attachment = await GetAuthorizedAttachmentAsync(
            attachmentId,
            userId,
            canViewAll,
            cancellationToken);

        if (attachment == null)
        {
            return (false, "Attachment was not found or you are not authorized to delete it.");
        }

        var path = GetSafePhysicalPath(attachment.FilePath);
        _context.TicketAttachments.Remove(attachment);
        await _context.SaveChangesAsync(cancellationToken);
        DeletePhysicalFile(path);
        return (true, null);
    }

    private async Task<TicketAttachment> SaveFileAsync(
        int ticketId,
        IFormFile file,
        ICollection<string> physicalPaths,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new InvalidDataException("Empty files are not allowed.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidDataException("The attachment exceeds the configured file size limit.");
        }

        var originalName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        var allowedExtensions = _options.AllowedExtensions
            .Select(e => e.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedContentTypes = _options.AllowedContentTypes
            .Select(t => t.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(originalName) || !allowedExtensions.Contains(extension))
        {
            throw new InvalidDataException("This attachment type is not allowed.");
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !allowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidDataException("The attachment content type is not allowed.");
        }

        await using var input = file.OpenReadStream();
        if (!await HasValidSignatureAsync(input, extension, cancellationToken))
        {
            throw new InvalidDataException("The attachment content does not match its declared file type.");
        }

        Directory.CreateDirectory(_storageRoot);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = GetSafePhysicalPath(storedName);
        await using var output = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await input.CopyToAsync(output, cancellationToken);
        physicalPaths.Add(physicalPath);

        return new TicketAttachment
        {
            MaintenanceTicketId = ticketId,
            FileName = originalName,
            FilePath = storedName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };
    }

    private async Task<TicketAttachment?> GetAuthorizedAttachmentAsync(
        int attachmentId,
        string userId,
        bool canViewAll,
        CancellationToken cancellationToken)
    {
        var query = _context.TicketAttachments
            .Include(a => a.MaintenanceTicket)
            .Where(a => a.Id == attachmentId);

        if (!canViewAll)
        {
            query = query.Where(a => a.MaintenanceTicket.SubmittedByUserId == userId);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private string GetSafePhysicalPath(string storedName)
    {
        var fileName = Path.GetFileName(storedName);
        if (!string.Equals(fileName, storedName, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidDataException("Invalid attachment path.");
        }

        var path = Path.GetFullPath(Path.Combine(_storageRoot, fileName));
        var root = _storageRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Invalid attachment path.");
        }

        return path;
    }

    private static async Task<bool> HasValidSignatureAsync(
        Stream stream,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        stream.Position = 0;

        return extension switch
        {
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".pdf" => read >= 5 && Encoding.ASCII.GetString(header, 0, 5) == "%PDF-",
            ".doc" => read >= 4 && header.SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }),
            ".docx" => read >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            _ => false
        };
    }

    private static void DeletePhysicalFile(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch
        {
            // Database metadata remains authoritative; cleanup can be retried separately.
        }
    }
}
