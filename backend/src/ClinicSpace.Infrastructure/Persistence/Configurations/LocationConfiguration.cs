using ClinicSpace.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicSpace.Infrastructure.Persistence.Configurations;

internal sealed class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("Regions");
        builder.Property(r => r.Name).HasMaxLength(Region.NameMaxLength).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.IsActive).HasDefaultValue(true);
    }
}

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites");
        builder.Property(s => s.Name).HasMaxLength(Site.NameMaxLength).IsRequired();
        builder.HasIndex(s => new { s.RegionId, s.Name }).IsUnique();
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.HasOne(s => s.Region).WithMany(r => r.Sites).HasForeignKey(s => s.RegionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("Buildings");
        builder.Property(b => b.Name).HasMaxLength(Building.NameMaxLength).IsRequired();
        builder.HasIndex(b => new { b.SiteId, b.Name }).IsUnique();
        builder.Property(b => b.IsActive).HasDefaultValue(true);
        builder.HasOne(b => b.Site).WithMany(s => s.Buildings).HasForeignKey(b => b.SiteId).OnDelete(DeleteBehavior.Restrict);
    }
}
