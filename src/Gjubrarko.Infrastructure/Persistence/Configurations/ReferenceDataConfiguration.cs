using Gjubrarko.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gjubrarko.Infrastructure.Persistence.Configurations;

public class MunicipalityConfiguration : IEntityTypeConfiguration<Municipality>
{
    public void Configure(EntityTypeBuilder<Municipality> b)
    {
        b.HasIndex(m => m.Slug).IsUnique();
        b.Property(m => m.Slug).HasMaxLength(64);
        b.Property(m => m.NameMk).HasMaxLength(100);
        b.Property(m => m.NameSq).HasMaxLength(100);
        b.Property(m => m.NameEn).HasMaxLength(100);
        b.Property(m => m.Centroid).HasColumnType("geometry(Point,4326)");
        b.Property(m => m.Boundary).HasColumnType("geometry(MultiPolygon,4326)");
        b.HasOne(m => m.Parent).WithMany().HasForeignKey(m => m.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(m => m.Region).WithMany(r => r.Municipalities).HasForeignKey(m => m.RegionId);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.HasIndex(c => c.Code).IsUnique();
        b.Property(c => c.Code).HasMaxLength(64);
    }
}

public class AuthorityTypeConfiguration : IEntityTypeConfiguration<AuthorityType>
{
    public void Configure(EntityTypeBuilder<AuthorityType> b)
    {
        b.HasIndex(t => t.Code).IsUnique();
        b.Property(t => t.Code).HasMaxLength(64);
        b.Property(t => t.Scope).HasConversion<string>().HasMaxLength(16);
    }
}

public class AuthorityConfiguration : IEntityTypeConfiguration<Authority>
{
    public void Configure(EntityTypeBuilder<Authority> b)
    {
        b.Property(a => a.Name).HasMaxLength(200);
        b.HasIndex(a => new { a.MunicipalityId, a.AuthorityTypeId });
    }
}
