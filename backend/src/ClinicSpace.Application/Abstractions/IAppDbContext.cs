using ClinicSpace.Domain.Locations;
using ClinicSpace.Domain.Notifications;
using ClinicSpace.Domain.Requests;
using ClinicSpace.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ClinicSpace.Application.Abstractions;

/// <summary>
/// The parts of the EF Core context that use cases need. EF Core's DbContext already acts as a
/// unit of work and DbSet as a repository, so no extra repository layer is added (see ADR 0004).
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<Region> Regions { get; }

    DbSet<Site> Sites { get; }

    DbSet<Building> Buildings { get; }

    DbSet<ClinicalSpaceRequest> Requests { get; }

    DbSet<RequestEvent> RequestEvents { get; }

    DbSet<EmailMessage> EmailMessages { get; }

    /// <summary>Sets the concurrency token value the client last saw, so a stale update is detected.</summary>
    void SetOriginalRowVersion(ClinicalSpaceRequest request, byte[] rowVersion);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
