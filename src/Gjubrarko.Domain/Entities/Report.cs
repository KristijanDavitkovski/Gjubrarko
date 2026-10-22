using Gjubrarko.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Gjubrarko.Domain.Entities;

public class Report
{
    public const int MaxPhotos = 5;
    public static readonly TimeSpan EditWindow = TimeSpan.FromHours(24);

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string PublicNumber { get; set; }

    public int MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }
    public required Point Location { get; set; }
    public string? AddressText { get; set; }
    public Severity Severity { get; set; } = Severity.Medium;
    public ReportStatus Status { get; set; } = ReportStatus.New;

    // Set when the reporter was signed in. Never exposed on public endpoints when IsAnonymous is true.
    public Guid? ReporterUserId { get; set; }
    public bool IsAnonymous { get; set; }
    // SHA-256 of the tracking token handed to an anonymous reporter; the token itself is never stored.
    public string? AnonymousTokenHash { get; set; }
    public string? ContactEmail { get; set; }

    // Reports sent without an account stay off the public site until a moderator approves them.
    public bool IsApproved { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public bool IsHidden { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }

    public List<ReportPhoto> Photos { get; set; } = [];
    public List<ReportAssignment> Assignments { get; set; } = [];
    public List<ReportStatusHistory> StatusHistory { get; set; } = [];

    public bool IsPublic => IsApproved && !IsHidden;

    public bool IsEditable(DateTimeOffset now) => now - CreatedAt <= EditWindow;

    public void Approve(Guid moderatorId, DateTimeOffset now)
    {
        IsApproved = true;
        ApprovedAt = now;
        ApprovedByUserId = moderatorId;
        UpdatedAt = now;
    }

    public void ChangeStatus(ReportStatus to, Guid? changedBy, string? note, DateTimeOffset now)
    {
        if (to == Status) return;
        StatusHistory.Add(new ReportStatusHistory
        {
            ReportId = Id, FromStatus = Status, ToStatus = to,
            ChangedByUserId = changedBy, Note = note, ChangedAt = now,
        });
        Status = to;
        UpdatedAt = now;
        ResolvedAt = to == ReportStatus.Resolved ? now : null;
    }
}
