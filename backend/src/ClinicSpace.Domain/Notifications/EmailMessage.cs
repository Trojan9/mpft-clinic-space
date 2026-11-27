namespace ClinicSpace.Domain.Notifications;

/// <summary>
/// A queued email (transactional outbox). It is saved in the same database transaction as the
/// change that caused it, so a decision can never be saved without its notification.
/// A background worker sends it and retries on failure.
/// </summary>
public class EmailMessage
{
    public const int SubjectMaxLength = 200;
    public const int LastErrorMaxLength = 2000;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string ToAddress { get; set; } = string.Empty;

    public string ToName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string TextBody { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>Request this email is about, if any. Used for traceability.</summary>
    public Guid? RelatedRequestId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset NextAttemptAtUtc { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }

    /// <summary>True once all retries are used up. Failed emails need manual follow-up.</summary>
    public bool IsDeadLettered { get; set; }

    public void MarkSent(DateTimeOffset now)
    {
        SentAtUtc = now;
        LastError = null;
    }

    /// <summary>Records a failed attempt and schedules the next one with exponential back-off.</summary>
    public void MarkFailed(string error, DateTimeOffset now, int maxAttempts)
    {
        Attempts++;
        LastError = error.Length > LastErrorMaxLength ? error[..LastErrorMaxLength] : error;
        if (Attempts >= maxAttempts)
        {
            IsDeadLettered = true;
            return;
        }

        // 30s, 1m, 2m, 4m ... capped at 30 minutes.
        var delaySeconds = Math.Min(30 * Math.Pow(2, Attempts - 1), 1800);
        NextAttemptAtUtc = now.AddSeconds(delaySeconds);
    }
}
