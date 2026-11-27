using ClinicSpace.Domain.Requests;
using ClinicSpace.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicSpace.Infrastructure.Persistence.Configurations;

internal sealed class RequestConfiguration : IEntityTypeConfiguration<ClinicalSpaceRequest>
{
    public void Configure(EntityTypeBuilder<ClinicalSpaceRequest> builder)
    {
        builder.ToTable("ClinicalSpaceRequests", t =>
        {
            // Rules enforced by the database as well as the application.
            t.HasCheckConstraint("CK_Requests_Period", "[EndsAt] > [StartsAt]");
            t.HasCheckConstraint("CK_Requests_Status", "[Status] IN ('Submitted', 'Accepted', 'Rejected')");
            t.HasCheckConstraint(
                "CK_Requests_Decision",
                "([Status] = 'Submitted' AND [DecidedById] IS NULL AND [DecidedAtUtc] IS NULL) OR ([Status] <> 'Submitted' AND [DecidedById] IS NOT NULL AND [DecidedAtUtc] IS NOT NULL)");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReferenceNumber)
            .HasDefaultValueSql($"NEXT VALUE FOR [{AppDbContext.ReferenceSequenceName}]")
            .ValueGeneratedOnAdd();
        builder.Property(r => r.ReferenceNumber).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        builder.HasIndex(r => r.ReferenceNumber).IsUnique();
        builder.Ignore(r => r.Reference);

        builder.Property(r => r.RequesterName).HasMaxLength(User.DisplayNameMaxLength).IsRequired();
        builder.Property(r => r.RequesterEmail).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.Property(r => r.CostCode).HasMaxLength(ClinicalSpaceRequest.CostCodeMaxLength).IsRequired();
        builder.Property(r => r.BudgetHolderName).HasMaxLength(ClinicalSpaceRequest.BudgetHolderNameMaxLength).IsRequired();
        builder.Property(r => r.BudgetHolderEmail).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.Property(r => r.AdditionalRequirementsNotes).HasMaxLength(AdditionalRequirements.NotesMaxLength);
        builder.Property(r => r.TermsVersion).HasMaxLength(32).IsRequired();
        builder.Property(r => r.DecisionNotes).HasMaxLength(ClinicalSpaceRequest.DecisionNotesMaxLength);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasOne(r => r.Requester).WithMany().HasForeignKey(r => r.RequesterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.DecidedBy).WithMany().HasForeignKey(r => r.DecidedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Building).WithMany().HasForeignKey(r => r.BuildingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Events).WithOne().HasForeignKey(e => e.RequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Events).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes for the queries the app actually runs.
        builder.HasIndex(r => new { r.RequesterId, r.CreatedAtUtc });
        builder.HasIndex(r => new { r.Status, r.CreatedAtUtc });
        builder.HasIndex(r => new { r.BuildingId, r.StartsAt, r.EndsAt });
    }
}

internal sealed class RequestEventConfiguration : IEntityTypeConfiguration<RequestEvent>
{
    public void Configure(EntityTypeBuilder<RequestEvent> builder)
    {
        builder.ToTable("RequestEvents", t =>
            t.HasCheckConstraint("CK_RequestEvents_Type", "[Type] IN ('Submitted', 'Accepted', 'Rejected')"));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.ActorName).HasMaxLength(User.DisplayNameMaxLength).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(RequestEvent.NotesMaxLength);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.RequestId, e.OccurredAtUtc });
    }
}
