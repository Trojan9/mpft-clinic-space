using System.Net;
using System.Text;
using ClinicSpace.Application.Common;
using ClinicSpace.Domain.Notifications;
using ClinicSpace.Domain.Requests;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Application.Notifications;

/// <summary>
/// Builds the emails sent about a request. Every email has a plain-text and an HTML part.
/// All user-entered values are HTML-encoded, so a malicious value cannot inject markup or links.
/// Content follows GOV.UK Notify / NHS guidance: plain English, the key fact first, one clear action.
/// </summary>
public sealed class RequestEmailFactory(IOptions<NotificationOptions> options, TimeProvider clock)
{
    private readonly NotificationOptions _options = options.Value;

    public EmailMessage Submitted(ClinicalSpaceRequest request, string locationDescription) =>
        Build(
            request,
            subject: $"We have received your clinical space request {request.Reference}",
            heading: "Request received",
            intro: "We have received your request for a clinical space. An administrator will review it and we will email you when a decision has been made.",
            outcomeLine: null,
            locationDescription);

    public EmailMessage Decided(ClinicalSpaceRequest request, string locationDescription)
    {
        var accepted = request.Status == RequestStatus.Accepted;
        if (!accepted && request.Status != RequestStatus.Rejected)
        {
            throw new InvalidOperationException("Only decided requests can generate a decision email.");
        }

        return Build(
            request,
            subject: accepted
                ? $"Your clinical space request {request.Reference} has been accepted"
                : $"Your clinical space request {request.Reference} has been rejected",
            heading: accepted ? "Request accepted" : "Request rejected",
            intro: accepted
                ? "Your request for a clinical space has been accepted."
                : "Your request for a clinical space has been rejected.",
            outcomeLine: request.DecisionNotes is null
                ? null
                : (accepted ? "Notes from the administrator: " : "Reason: ") + request.DecisionNotes,
            locationDescription);
    }

    private EmailMessage Build(
        ClinicalSpaceRequest request,
        string subject,
        string heading,
        string intro,
        string? outcomeLine,
        string locationDescription)
    {
        var link = $"{_options.PublicBaseUrl.TrimEnd('/')}/requests/{request.Id}";
        var period = UkDateFormatter.FormatPeriod(request.StartsAt, request.EndsAt);
        var facilities = DescribeFacilities(request);

        var details = new List<(string Label, string Value)>
        {
            ("Reference", request.Reference),
            ("Location", locationDescription),
            ("When", period),
            ("Facilities", facilities),
            ("Cost code", request.CostCode),
        };

        var text = new StringBuilder()
            .AppendLine($"Dear {request.RequesterName},")
            .AppendLine()
            .AppendLine(intro)
            .AppendLine();
        if (outcomeLine is not null)
        {
            text.AppendLine(outcomeLine).AppendLine();
        }

        foreach (var (label, value) in details)
        {
            text.AppendLine($"{label}: {value}");
        }

        text.AppendLine()
            .AppendLine($"View your request: {link}")
            .AppendLine()
            .AppendLine($"{_options.ServiceName}")
            .AppendLine("This is an automated message. Please do not reply.");

        var html = new StringBuilder()
            .Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>")
            .Append(Encode(subject))
            .Append("</title></head><body style=\"margin:0;padding:0;background:#f0f4f5;font-family:Arial,sans-serif;color:#212b32;\">")
            .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f0f4f5;\"><tr><td align=\"center\" style=\"padding:24px 12px;\">")
            .Append("<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;width:100%;background:#ffffff;\">")
            .Append("<tr><td style=\"background:#005eb8;padding:16px 24px;color:#ffffff;font-size:20px;font-weight:bold;\">")
            .Append(Encode(_options.ServiceName))
            .Append("</td></tr><tr><td style=\"padding:24px;font-size:16px;line-height:1.5;\">")
            .Append("<h1 style=\"font-size:28px;margin:0 0 16px;\">").Append(Encode(heading)).Append("</h1>")
            .Append("<p style=\"margin:0 0 16px;\">Dear ").Append(Encode(request.RequesterName)).Append(",</p>")
            .Append("<p style=\"margin:0 0 16px;\">").Append(Encode(intro)).Append("</p>");
        if (outcomeLine is not null)
        {
            html.Append("<p style=\"margin:0 0 16px;padding:12px 16px;border-left:8px solid #005eb8;background:#f0f4f5;\">")
                .Append(Encode(outcomeLine))
                .Append("</p>");
        }

        html.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;border-collapse:collapse;margin:0 0 24px;\">");
        foreach (var (label, value) in details)
        {
            html.Append("<tr><th scope=\"row\" style=\"text-align:left;vertical-align:top;padding:8px 16px 8px 0;border-bottom:1px solid #d8dde0;width:30%;\">")
                .Append(Encode(label))
                .Append("</th><td style=\"padding:8px 0;border-bottom:1px solid #d8dde0;\">")
                .Append(Encode(value))
                .Append("</td></tr>");
        }

        html.Append("</table>")
            .Append("<p style=\"margin:0 0 24px;\"><a href=\"").Append(Encode(link))
            .Append("\" style=\"display:inline-block;background:#007f3b;color:#ffffff;padding:12px 16px;text-decoration:none;font-weight:bold;border-radius:4px;\">View your request</a></p>")
            .Append("<p style=\"margin:0;color:#4c6272;font-size:14px;\">This is an automated message. Please do not reply.</p>")
            .Append("</td></tr></table></td></tr></table></body></html>");

        var now = clock.GetUtcNow();
        return new EmailMessage
        {
            ToAddress = request.RequesterEmail,
            ToName = request.RequesterName,
            Subject = subject.Length > EmailMessage.SubjectMaxLength ? subject[..EmailMessage.SubjectMaxLength] : subject,
            TextBody = text.ToString(),
            HtmlBody = html.ToString(),
            RelatedRequestId = request.Id,
            CreatedAtUtc = now,
            NextAttemptAtUtc = now,
        };
    }

    internal static string DescribeFacilities(ClinicalSpaceRequest request)
    {
        var parts = new List<string>
        {
            request.SinkRequired ? "Sink required" : "No sink required",
            request.ClinicalCouchRequired ? "Clinical couch required" : "No clinical couch required",
        };
        if (!string.IsNullOrWhiteSpace(request.AdditionalRequirementsNotes))
        {
            parts.Add($"Other: {request.AdditionalRequirementsNotes}");
        }

        return string.Join("; ", parts);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
