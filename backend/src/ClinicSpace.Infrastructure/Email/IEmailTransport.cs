using ClinicSpace.Domain.Notifications;

namespace ClinicSpace.Infrastructure.Email;

/// <summary>Delivers one email. Implementations throw on failure; the outbox handles retries.</summary>
public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
