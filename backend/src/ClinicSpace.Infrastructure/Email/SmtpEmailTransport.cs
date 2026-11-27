using ClinicSpace.Domain.Notifications;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Infrastructure.Email;

/// <summary>Sends email over SMTP with MailKit (the client Microsoft recommends over System.Net.Mail.SmtpClient).</summary>
internal sealed class SmtpEmailTransport(IOptions<EmailOptions> options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new SmtpClient();
        var security = Enum.TryParse<SecureSocketOptions>(settings.SmtpSecurity, ignoreCase: true, out var parsed)
            ? parsed
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, security, cancellationToken);
        if (!string.IsNullOrEmpty(settings.SmtpUsername))
        {
            await client.AuthenticateAsync(settings.SmtpUsername, settings.SmtpPassword ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(MimeMessageFactory.Create(message, settings), cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
