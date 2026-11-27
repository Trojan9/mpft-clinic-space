using ClinicSpace.Domain.Notifications;
using ClinicSpace.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicSpace.Infrastructure.Persistence.Configurations;

internal sealed class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("EmailOutbox");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ToAddress).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.Property(m => m.ToName).HasMaxLength(User.DisplayNameMaxLength).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(EmailMessage.SubjectMaxLength).IsRequired();
        builder.Property(m => m.TextBody).IsRequired();
        builder.Property(m => m.HtmlBody).IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(EmailMessage.LastErrorMaxLength);

        // The dispatcher only ever looks for unsent, live messages that are due.
        builder.HasIndex(m => m.NextAttemptAtUtc)
            .HasFilter("[SentAtUtc] IS NULL AND [IsDeadLettered] = 0")
            .HasDatabaseName("IX_EmailOutbox_Pending");
        builder.HasIndex(m => m.RelatedRequestId);
    }
}
