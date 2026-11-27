namespace ClinicSpace.Domain.Requests;

/// <summary>
/// An append-only audit record of something that happened to a request: who, what and when.
/// Shown to users as the request timeline.
/// </summary>
public class RequestEvent
{
    public const int NotesMaxLength = 1000;

    public long Id { get; set; }

    public Guid RequestId { get; set; }

    public RequestEventType Type { get; set; }

    public Guid ActorUserId { get; set; }

    /// <summary>Snapshot of the actor's display name at the time, so history stays readable.</summary>
    public string ActorName { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? Notes { get; set; }
}
