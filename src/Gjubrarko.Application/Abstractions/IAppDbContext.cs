using Gjubrarko.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gjubrarko.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Region> Regions { get; }
    DbSet<Municipality> Municipalities { get; }
    DbSet<Category> Categories { get; }
    DbSet<AuthorityType> AuthorityTypes { get; }
    DbSet<Authority> Authorities { get; }
    DbSet<Report> Reports { get; }
    DbSet<ReportPhoto> ReportPhotos { get; }
    DbSet<ModerationFlag> ModerationFlags { get; }

    Task<long> NextReportSequenceAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
