using Gjubrarko.Application.Abstractions;
using Gjubrarko.Application.Common;
using Gjubrarko.Domain.Entities;
using Gjubrarko.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Gjubrarko.Application.Reports;

public class ReportService(IAppDbContext db, IPhotoStorage storage, IImageProcessor images, IUserDirectory users, TimeProvider clock)
{
    public const long MaxPhotoBytes = 10 * 1024 * 1024;
    private const string AnonymousName = "Anonymous citizen";
    private static readonly GeometryFactory Geometry = new(new PrecisionModel(), 4326);

    public async Task<CreateReportResult> CreateAsync(CreateReportRequest request, IReadOnlyList<PhotoUpload> photos, Guid? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 120)
            throw AppException.BadRequest("Title is required and must be at most 120 characters.");
        if (photos.Count > Report.MaxPhotos)
            throw AppException.BadRequest($"At most {Report.MaxPhotos} photos are allowed.");
        if (request.AuthorityIds.Length == 0)
            throw AppException.BadRequest("Choose at least one responsible authority.");

        var municipality = await db.Municipalities.FindAsync([request.MunicipalityId], ct)
            ?? throw AppException.NotFound("Municipality");
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
            throw AppException.NotFound("Category");

        var location = Geometry.CreatePoint(new Coordinate(request.Longitude, request.Latitude));
        if (municipality.Boundary is not null && !municipality.Boundary.Contains(location))
            throw AppException.BadRequest("The pin is outside the chosen municipality.");

        // An authority must be national or belong to this municipality (or to the City of Skopje for its parts).
        var authorities = await db.Authorities
            .Where(a => request.AuthorityIds.Contains(a.Id) && a.IsActive)
            .Where(a => a.MunicipalityId == null || a.MunicipalityId == municipality.Id || a.MunicipalityId == municipality.ParentId)
            .ToListAsync(ct);
        if (authorities.Count != request.AuthorityIds.Distinct().Count())
            throw AppException.BadRequest("One or more authorities do not serve this municipality.");

        var now = clock.GetUtcNow();
        var anonymous = request.Anonymous || userId is null;
        string? token = userId is null ? AnonymousToken.Create() : null;

        var report = new Report
        {
            PublicNumber = $"MK-{now.Year}-{await db.NextReportSequenceAsync(ct):D6}",
            MunicipalityId = municipality.Id,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Location = location,
            AddressText = request.AddressText,
            Severity = request.Severity,
            ReporterUserId = userId,
            IsAnonymous = anonymous,
            AnonymousTokenHash = token is null ? null : AnonymousToken.Hash(token),
            IsApproved = userId is not null,
            ContactEmail = userId is null ? request.ContactEmail : null,
            CreatedAt = now,
            UpdatedAt = now,
        };
        report.Assignments.AddRange(authorities.Select(a => new ReportAssignment
        {
            ReportId = report.Id, AuthorityId = a.Id, Source = AssignmentSource.Citizen, AssignedAt = now,
        }));

        foreach (var photo in photos)
            report.Photos.Add(await StorePhotoAsync(report.Id, photo, PhotoKind.Report, ct));

        db.Reports.Add(report);
        await db.SaveChangesAsync(ct);
        return new CreateReportResult(report.Id, report.PublicNumber, token);
    }

    public async Task AddPhotosAsync(Guid reportId, IReadOnlyList<PhotoUpload> photos, Guid? userId, string? token, CancellationToken ct)
    {
        var report = await db.Reports.Include(r => r.Photos).FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw AppException.NotFound("Report");
        EnsureOwner(report, userId, token);
        if (!report.IsEditable(clock.GetUtcNow()))
            throw AppException.Forbidden("Reports can only be changed within 24 hours.");
        if (report.Photos.Count(p => p.Kind == PhotoKind.Report) + photos.Count > Report.MaxPhotos)
            throw AppException.BadRequest($"At most {Report.MaxPhotos} photos are allowed.");

        foreach (var photo in photos)
            report.Photos.Add(await StorePhotoAsync(report.Id, photo, PhotoKind.Report, ct));
        report.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ReportListItem>> ListAsync(ReportQuery q, CancellationToken ct)
    {
        var query = db.Reports.AsNoTracking().Where(r => r.IsApproved && !r.IsHidden);
        if (q.MunicipalityId is { } m) query = query.Where(r => r.MunicipalityId == m || r.Municipality!.ParentId == m);
        if (q.CategoryId is { } c) query = query.Where(r => r.CategoryId == c);
        if (q.Status is { } s) query = query.Where(r => r.Status == s);
        if (q is { MinLat: { } minLat, MinLng: { } minLng, MaxLat: { } maxLat, MaxLng: { } maxLng })
        {
            var box = Geometry.ToGeometry(new Envelope(minLng, maxLng, minLat, maxLat));
            query = query.Where(r => box.Contains(r.Location));
        }

        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var page = Math.Max(q.Page, 1);
        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new
            {
                r.Id, r.PublicNumber, r.Title, Municipality = r.Municipality!.NameMk, Category = r.Category!.NameMk,
                r.Status, r.Location, r.CreatedAt,
                Thumb = r.Photos.Where(p => !p.IsHidden).OrderBy(p => p.UploadedAt).Select(p => p.ThumbnailKey).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new ReportListItem(
            r.Id, r.PublicNumber, r.Title, r.Municipality, r.Category, r.Status, r.Location.Y, r.Location.X,
            r.Thumb is null ? null : storage.GetReadUrl(r.Thumb), r.CreatedAt)).ToList();
        return new PagedResult<ReportListItem>(items, page, pageSize, total);
    }

    /// <summary>Pending reports are visible only to their reporter (account or tracking token) and to moderators.</summary>
    public async Task<ReportDetail> GetAsync(Guid id, Guid? userId, string? token, bool isModerator, CancellationToken ct)
    {
        var r = await db.Reports.AsNoTracking()
            .Include(x => x.Municipality).Include(x => x.Category)
            .Include(x => x.Photos).Include(x => x.Assignments).ThenInclude(a => a.Authority)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Report");
        if (!r.IsPublic && !isModerator && !IsOwner(r, userId, token))
            throw AppException.NotFound("Report");

        var reporter = r.IsAnonymous || r.ReporterUserId is null
            ? AnonymousName
            : await users.GetDisplayNameAsync(r.ReporterUserId.Value, ct) ?? AnonymousName;

        return new ReportDetail(
            r.Id, r.PublicNumber, r.Title, r.Description,
            r.MunicipalityId, r.Municipality!.NameMk, r.CategoryId, r.Category!.NameMk,
            r.Location.Y, r.Location.X, r.AddressText, r.Severity, r.Status,
            reporter, r.CreatedAt, r.ResolvedAt,
            r.Photos.Where(p => !p.IsHidden).OrderBy(p => p.UploadedAt)
                .Select(p => new PhotoDto(p.Id, storage.GetReadUrl(p.StorageKey), storage.GetReadUrl(p.ThumbnailKey), p.Kind)).ToList(),
            r.Assignments.Select(a => new AssignmentDto(a.AuthorityId, a.Authority!.Name, a.AssignedAt, a.AcceptedAt)).ToList(),
            r.StatusHistory.OrderBy(h => h.ChangedAt).Select(h => new StatusChangeDto(h.FromStatus, h.ToStatus, h.Note, h.ChangedAt)).ToList());
    }

    private static bool IsOwner(Report report, Guid? userId, string? token) =>
        (userId is not null && report.ReporterUserId == userId)
        || (token is not null && AnonymousToken.Matches(token, report.AnonymousTokenHash));

    private static void EnsureOwner(Report report, Guid? userId, string? token)
    {
        if (!IsOwner(report, userId, token)) throw AppException.Forbidden("Only the reporter can change this report.");
    }

    private async Task<ReportPhoto> StorePhotoAsync(Guid reportId, PhotoUpload upload, PhotoKind kind, CancellationToken ct)
    {
        if (upload.Length > MaxPhotoBytes)
            throw AppException.BadRequest($"{upload.FileName} is larger than 10 MB.");

        var processed = await images.ProcessAsync(upload.Content, ct);
        var photo = new ReportPhoto
        {
            ReportId = reportId,
            StorageKey = string.Empty,
            ThumbnailKey = string.Empty,
            Width = processed.Width,
            Height = processed.Height,
            SizeBytes = processed.Full.Length,
            Kind = kind,
            UploadedAt = clock.GetUtcNow(),
        };
        photo.StorageKey = $"reports/{reportId}/{photo.Id}.webp";
        photo.ThumbnailKey = $"reports/{reportId}/{photo.Id}_thumb.webp";

        await storage.UploadAsync(photo.StorageKey, new MemoryStream(processed.Full), photo.ContentType, ct);
        await storage.UploadAsync(photo.ThumbnailKey, new MemoryStream(processed.Thumbnail), photo.ContentType, ct);
        return photo;
    }
}
