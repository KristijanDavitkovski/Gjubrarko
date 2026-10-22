using Gjubrarko.Application.Abstractions;
using Gjubrarko.Domain.Entities;
using Gjubrarko.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gjubrarko.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    public const string ReportNumberSequence = "report_number_seq";

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AuthorityType> AuthorityTypes => Set<AuthorityType>();
    public DbSet<Authority> Authorities => Set<Authority>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportPhoto> ReportPhotos => Set<ReportPhoto>();
    public DbSet<ModerationFlag> ModerationFlags => Set<ModerationFlag>();

    public async Task<long> NextReportSequenceAsync(CancellationToken cancellationToken = default) =>
        await Database.SqlQueryRaw<long>($"SELECT nextval('{ReportNumberSequence}') AS \"Value\"").SingleAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("postgis");
        builder.HasSequence<long>(ReportNumberSequence);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
