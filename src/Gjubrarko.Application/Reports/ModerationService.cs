using Gjubrarko.Application.Abstractions;
using Gjubrarko.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Gjubrarko.Application.Reports;

public sealed record PendingReportItem(Guid Id, string PublicNumber, string Title, string Municipality, DateTimeOffset CreatedAt, int PhotoCount);

public class ModerationService(IAppDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<PendingReportItem>> PendingAsync(CancellationToken ct) =>
        await db.Reports.AsNoTracking()
            .Where(r => !r.IsApproved && !r.IsHidden)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new PendingReportItem(r.Id, r.PublicNumber, r.Title, r.Municipality!.NameMk, r.CreatedAt, r.Photos.Count))
            .ToListAsync(ct);

    public async Task ApproveAsync(Guid reportId, Guid moderatorId, CancellationToken ct)
    {
        var report = await db.Reports.FindAsync([reportId], ct) ?? throw AppException.NotFound("Report");
        report.Approve(moderatorId, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    public async Task SetHiddenAsync(Guid reportId, bool hidden, CancellationToken ct)
    {
        var report = await db.Reports.FindAsync([reportId], ct) ?? throw AppException.NotFound("Report");
        report.IsHidden = hidden;
        report.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }
}
