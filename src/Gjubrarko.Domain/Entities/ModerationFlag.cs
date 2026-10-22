namespace Gjubrarko.Domain.Entities;

public class ModerationFlag
{
    public int Id { get; set; }
    public Guid ReportId { get; set; }
    public Guid? PhotoId { get; set; }
    public required string Reason { get; set; }
    public Guid? ReportedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Resolution { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
