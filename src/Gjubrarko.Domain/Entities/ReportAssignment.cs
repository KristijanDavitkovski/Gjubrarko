using Gjubrarko.Domain.Enums;

namespace Gjubrarko.Domain.Entities;

public class ReportAssignment
{
    public int Id { get; set; }
    public Guid ReportId { get; set; }
    public int AuthorityId { get; set; }
    public Authority? Authority { get; set; }
    public AssignmentSource Source { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAt { get; set; }
}
