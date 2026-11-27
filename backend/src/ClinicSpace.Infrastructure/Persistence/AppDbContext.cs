using ClinicSpace.Application.Abstractions;
using ClinicSpace.Domain.Common;
using ClinicSpace.Domain.Locations;
using ClinicSpace.Domain.Notifications;
using ClinicSpace.Domain.Requests;
using ClinicSpace.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ClinicSpace.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider clock)
    : DbContext(options), IAppDbContext
{
    public const string ReferenceSequenceName = "RequestReferenceNumbers";

    public DbSet<User> Users => Set<User>();

    public DbSet<Region> Regions => Set<Region>();

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Building> Buildings => Set<Building>();

    public DbSet<ClinicalSpaceRequest> Requests => Set<ClinicalSpaceRequest>();

    public DbSet<RequestEvent> RequestEvents => Set<RequestEvent>();

    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();

    public void SetOriginalRowVersion(ClinicalSpaceRequest request, byte[] rowVersion) =>
        Entry(request).Property(r => r.RowVersion).OriginalValue = rowVersion;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>(ReferenceSequenceName).StartsAt(1).IncrementsBy(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>Sets created and updated timestamps on every auditable entity, so no use case can forget.</summary>
    private void StampAuditFields()
    {
        var now = clock.GetUtcNow();
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                // Seed data may set an earlier created time; normal use cases never do.
                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = now;
                }

                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
