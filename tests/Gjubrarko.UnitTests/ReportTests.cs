using Gjubrarko.Domain.Entities;
using Gjubrarko.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Gjubrarko.UnitTests;

public class ReportTests
{
    private static Report NewReport(DateTimeOffset createdAt) => new()
    {
        PublicNumber = "MK-2026-000001",
        Title = "Dump by the river",
        Location = new Point(21.43, 41.99) { SRID = 4326 },
        CreatedAt = createdAt,
    };

    [Fact]
    public void Status_changes_are_recorded_and_resolution_time_is_set()
    {
        var now = DateTimeOffset.UtcNow;
        var report = NewReport(now);
        var staff = Guid.NewGuid();

        report.ChangeStatus(ReportStatus.Received, staff, null, now);
        report.ChangeStatus(ReportStatus.Resolved, staff, "Cleaned up", now.AddDays(2));

        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.Equal(now.AddDays(2), report.ResolvedAt);
        Assert.Collection(report.StatusHistory,
            h => Assert.Equal((ReportStatus.New, ReportStatus.Received), (h.FromStatus, h.ToStatus)),
            h => Assert.Equal((ReportStatus.Received, ReportStatus.Resolved), (h.FromStatus, h.ToStatus)));
    }

    [Fact]
    public void Report_is_editable_only_for_24_hours()
    {
        var created = DateTimeOffset.UtcNow;
        var report = NewReport(created);

        Assert.True(report.IsEditable(created.AddHours(23)));
        Assert.False(report.IsEditable(created.AddHours(25)));
    }

    [Fact]
    public void Unapproved_report_is_not_public_until_a_moderator_approves_it()
    {
        var report = NewReport(DateTimeOffset.UtcNow);
        Assert.False(report.IsPublic);

        report.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.True(report.IsPublic);
    }
}
