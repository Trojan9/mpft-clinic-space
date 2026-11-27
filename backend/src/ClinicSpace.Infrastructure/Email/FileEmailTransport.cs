using ClinicSpace.Domain.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Infrastructure.Email;

/// <summary>Writes each email to an .eml file, so notifications can be checked with no mail server.</summary>
internal sealed class FileEmailTransport(IOptions<EmailOptions> options, ILogger<FileEmailTransport> logger) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var directory = Path.GetFullPath(settings.FileDirectory);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{message.CreatedAtUtc:yyyyMMdd-HHmmss}-{message.Id:N}.eml");
        await MimeMessageFactory.Create(message, settings).WriteToAsync(path, cancellationToken);
        logger.LogInformation("Email {EmailId} written to {Path}", message.Id, path);
    }
}
