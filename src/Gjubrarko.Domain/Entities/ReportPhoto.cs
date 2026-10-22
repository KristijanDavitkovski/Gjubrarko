using Gjubrarko.Domain.Enums;

namespace Gjubrarko.Domain.Entities;

public class ReportPhoto
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ReportId { get; set; }
    public required string StorageKey { get; set; }
    public required string ThumbnailKey { get; set; }
    public string ContentType { get; set; } = "image/webp";
    public int Width { get; set; }
    public int Height { get; set; }
    public long SizeBytes { get; set; }
    public PhotoKind Kind { get; set; } = PhotoKind.Report;
    public bool IsHidden { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
