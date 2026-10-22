using Gjubrarko.Domain.Enums;

namespace Gjubrarko.Application.Reports;

public sealed record CreateReportRequest(
    int MunicipalityId,
    int CategoryId,
    string Title,
    string? Description,
    double Latitude,
    double Longitude,
    string? AddressText,
    Severity Severity,
    int[] AuthorityIds,
    bool Anonymous,
    string? ContactEmail);

public sealed record PhotoUpload(Stream Content, string FileName, long Length);

public sealed record CreateReportResult(Guid Id, string PublicNumber, string? TrackingToken);

public sealed record ReportQuery(
    int? MunicipalityId = null,
    int? CategoryId = null,
    ReportStatus? Status = null,
    double? MinLat = null, double? MinLng = null, double? MaxLat = null, double? MaxLng = null,
    int Page = 1,
    int PageSize = 20);

public sealed record ReportListItem(
    Guid Id, string PublicNumber, string Title, string Municipality, string Category,
    ReportStatus Status, double Latitude, double Longitude, string? ThumbnailUrl, DateTimeOffset CreatedAt);

public sealed record PhotoDto(Guid Id, string Url, string ThumbnailUrl, PhotoKind Kind);
public sealed record AssignmentDto(int AuthorityId, string AuthorityName, DateTimeOffset AssignedAt, DateTimeOffset? AcceptedAt);
public sealed record StatusChangeDto(ReportStatus From, ReportStatus To, string? Note, DateTimeOffset ChangedAt);

public sealed record ReportDetail(
    Guid Id, string PublicNumber, string Title, string? Description,
    int MunicipalityId, string Municipality, int CategoryId, string Category,
    double Latitude, double Longitude, string? AddressText, Severity Severity, ReportStatus Status,
    string ReporterName, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt,
    IReadOnlyList<PhotoDto> Photos, IReadOnlyList<AssignmentDto> Assignments, IReadOnlyList<StatusChangeDto> Timeline);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
