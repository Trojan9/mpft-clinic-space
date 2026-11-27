using ClinicSpace.Domain.Notifications;
using MimeKit;

namespace ClinicSpace.Infrastructure.Email;

internal static class MimeMessageFactory
{
    public static MimeMessage Create(EmailMessage message, EmailOptions options)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;
        mime.MessageId = $"{message.Id:N}@clinicspace";
        mime.Date = message.CreatedAtUtc;
        mime.Headers.Add("X-Clinic-Space-Request", message.RelatedRequestId?.ToString() ?? string.Empty);
        mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();
        return mime;
    }
}
