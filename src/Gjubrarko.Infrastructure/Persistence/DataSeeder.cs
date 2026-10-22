using System.Text.Json;
using System.Text.Json.Serialization;
using Gjubrarko.Domain.Entities;
using Gjubrarko.Domain.Enums;
using Gjubrarko.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gjubrarko.Infrastructure.Persistence;

/// <summary>Loads municipalities, authority types, categories and national authorities from the seed folder. Safe to run on every start.</summary>
public class DataSeeder(AppDbContext db, RoleManager<IdentityRole<Guid>> roles, ILogger<DataSeeder> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private sealed record MunicipalitySeed(string Slug, string NameEn, string NameMk, string NameSq, string? ParentSlug);
    private sealed record AuthorityTypeSeed(string Code, AuthorityScope Scope, string NameEn, string NameMk, string? NameSq);
    private sealed record CategorySeed(string Code, string DefaultAuthorityType, string? Icon, string NameEn, string NameMk, string? NameSq);

    public async Task SeedAsync(string seedFolder, CancellationToken ct = default)
    {
        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole<Guid>(role));

        await SeedMunicipalitiesAsync(Path.Combine(seedFolder, "municipalities.json"), ct);
        await SeedAuthorityTypesAsync(Path.Combine(seedFolder, "authority-types.json"), ct);
        await SeedCategoriesAsync(Path.Combine(seedFolder, "categories.json"), ct);
        await SeedNationalAuthoritiesAsync(ct);
    }

    private async Task SeedMunicipalitiesAsync(string path, CancellationToken ct)
    {
        if (await db.Municipalities.AnyAsync(ct)) return;
        var seeds = await Read<MunicipalitySeed>(path, ct);

        var bySlug = seeds.ToDictionary(s => s.Slug, s => new Municipality
        {
            Slug = s.Slug, NameEn = s.NameEn, NameMk = s.NameMk, NameSq = s.NameSq,
        });
        foreach (var s in seeds.Where(s => s.ParentSlug is not null))
            bySlug[s.Slug].Parent = bySlug[s.ParentSlug!];

        db.Municipalities.AddRange(bySlug.Values);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} municipalities", bySlug.Count);
    }

    private async Task SeedAuthorityTypesAsync(string path, CancellationToken ct)
    {
        if (await db.AuthorityTypes.AnyAsync(ct)) return;
        var seeds = await Read<AuthorityTypeSeed>(path, ct);
        db.AuthorityTypes.AddRange(seeds.Select(s => new AuthorityType
        {
            Code = s.Code, Scope = s.Scope, NameEn = s.NameEn, NameMk = s.NameMk, NameSq = s.NameSq ?? s.NameEn,
        }));
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedCategoriesAsync(string path, CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(ct)) return;
        var types = await db.AuthorityTypes.ToDictionaryAsync(t => t.Code, ct);
        var seeds = await Read<CategorySeed>(path, ct);
        db.Categories.AddRange(seeds.Select(s => new Category
        {
            Code = s.Code, Icon = s.Icon, NameEn = s.NameEn, NameMk = s.NameMk, NameSq = s.NameSq ?? s.NameEn,
            DefaultAuthorityTypeId = types[s.DefaultAuthorityType].Id,
        }));
        await db.SaveChangesAsync(ct);
    }

    // National bodies serve every municipality, so one row per type is enough. Municipal ones are added by admins.
    private async Task SeedNationalAuthoritiesAsync(CancellationToken ct)
    {
        if (await db.Authorities.AnyAsync(a => a.MunicipalityId == null, ct)) return;
        var national = await db.AuthorityTypes.Where(t => t.Scope == AuthorityScope.National).ToListAsync(ct);
        db.Authorities.AddRange(national.Select(t => new Authority { AuthorityTypeId = t.Id, Name = t.NameMk }));
        await db.SaveChangesAsync(ct);
    }

    private static async Task<List<T>> Read<T>(string path, CancellationToken ct)
    {
        await using var file = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(file, Json, ct) ?? [];
    }
}
