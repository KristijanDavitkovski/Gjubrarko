using Gjubrarko.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gjubrarko.Api.Controllers;

public sealed record MunicipalityDto(int Id, string Slug, string NameMk, string NameSq, string NameEn, int? ParentId, double? Latitude, double? Longitude);
public sealed record CategoryDto(int Id, string Code, string NameMk, string NameSq, string NameEn, string? Icon, int DefaultAuthorityTypeId);
public sealed record AuthorityDto(int Id, string Name, int AuthorityTypeId, string AuthorityType, bool IsNational);

[ApiController]
[Route("api/v1")]
public class ReferenceController(IAppDbContext db) : ControllerBase
{
    [HttpGet("municipalities")]
    public async Task<IReadOnlyList<MunicipalityDto>> Municipalities(CancellationToken ct) =>
        await db.Municipalities.AsNoTracking().OrderBy(m => m.NameMk)
            .Select(m => new MunicipalityDto(m.Id, m.Slug, m.NameMk, m.NameSq, m.NameEn, m.ParentId,
                m.Centroid == null ? null : m.Centroid.Y, m.Centroid == null ? null : m.Centroid.X))
            .ToListAsync(ct);

    [HttpGet("municipalities/{slug}/authorities")]
    public async Task<ActionResult<IReadOnlyList<AuthorityDto>>> Authorities(string slug, CancellationToken ct)
    {
        var m = await db.Municipalities.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (m is null) return NotFound();
        return await db.Authorities.AsNoTracking()
            .Where(a => a.IsActive && (a.MunicipalityId == null || a.MunicipalityId == m.Id || a.MunicipalityId == m.ParentId))
            .OrderBy(a => a.MunicipalityId == null).ThenBy(a => a.Name)
            .Select(a => new AuthorityDto(a.Id, a.Name, a.AuthorityTypeId, a.AuthorityType!.NameMk, a.MunicipalityId == null))
            .ToListAsync(ct);
    }

    [HttpGet("categories")]
    public async Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct) =>
        await db.Categories.AsNoTracking().OrderBy(c => c.Id)
            .Select(c => new CategoryDto(c.Id, c.Code, c.NameMk, c.NameSq, c.NameEn, c.Icon, c.DefaultAuthorityTypeId))
            .ToListAsync(ct);
}
