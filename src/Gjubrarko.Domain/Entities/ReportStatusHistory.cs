using Gjubrarko.Domain.Enums;

namespace Gjubrarko.Domain.Entities;

public class ReportStatusHistory
{
    public int Id { get; set; }
    public Guid ReportId { get; set; }
    public ReportStatus FromStatus { get; set; }
    public ReportStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
