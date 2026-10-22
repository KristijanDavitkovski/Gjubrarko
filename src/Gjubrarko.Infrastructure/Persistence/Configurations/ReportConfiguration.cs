using Gjubrarko.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gjubrarko.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.HasIndex(r => r.PublicNumber).IsUnique();
        b.Property(r => r.PublicNumber).HasMaxLength(32);
        b.Property(r => r.Title).HasMaxLength(120);
        b.Property(r => r.Description).HasMaxLength(4000);
        b.Property(r => r.AddressText).HasMaxLength(300);
        b.Property(r => r.AnonymousTokenHash).HasMaxLength(64);
        b.Property(r => r.ContactEmail).HasMaxLength(256);
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(r => r.Severity).HasConversion<string>().HasMaxLength(16);

        b.Property(r => r.Location).HasColumnType("geometry(Point,4326)");
        b.HasIndex(r => r.Location).HasMethod("gist");
        b.HasIndex(r => new { r.MunicipalityId, r.Status, r.CreatedAt });
        b.HasIndex(r => new { r.IsApproved, r.CreatedAt });

        b.HasMany(r => r.Photos).WithOne().HasForeignKey(p => p.ReportId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(r => r.Assignments).WithOne().HasForeignKey(a => a.ReportId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(r => r.StatusHistory).WithOne().HasForeignKey(h => h.ReportId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReportPhotoConfiguration : IEntityTypeConfiguration<ReportPhoto>
{
    public void Configure(EntityTypeBuilder<ReportPhoto> b)
    {
        b.Property(p => p.StorageKey).HasMaxLength(200);
        b.Property(p => p.ThumbnailKey).HasMaxLength(200);
        b.Property(p => p.Kind).HasConversion<string>().HasMaxLength(24);
    }
}

public class ReportAssignmentConfiguration : IEntityTypeConfiguration<ReportAssignment>
{
    public void Configure(EntityTypeBuilder<ReportAssignment> b)
    {
        b.HasIndex(a => new { a.ReportId, a.AuthorityId }).IsUnique();
        b.Property(a => a.Source).HasConversion<string>().HasMaxLength(16);
    }
}

public class ReportStatusHistoryConfiguration : IEntityTypeConfiguration<ReportStatusHistory>
{
    public void Configure(EntityTypeBuilder<ReportStatusHistory> b)
    {
        b.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(16);
        b.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(16);
        b.Property(h => h.Note).HasMaxLength(2000);
    }
}

public class ModerationFlagConfiguration : IEntityTypeConfiguration<ModerationFlag>
{
    public void Configure(EntityTypeBuilder<ModerationFlag> b)
    {
        b.Property(f => f.Reason).HasMaxLength(500);
        b.HasIndex(f => new { f.ReportId, f.ResolvedAt });
    }
}
