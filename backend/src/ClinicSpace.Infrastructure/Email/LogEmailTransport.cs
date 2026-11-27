using ClinicSpace.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace ClinicSpace.Infrastructure.Email;

/// <summary>Writes the email to the log. Useful in tests and when no other option is set.</summary>
internal sealed class LogEmailTransport(ILogger<LogEmailTransport> logger) : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "EMAIL (log mode) to {To} | Subject: {Subject}{NewLine}{Body}",
            message.ToAddress,
            message.Subject,
            Environment.NewLine,
            message.TextBody);
        return Task.CompletedTask;
    }
}
