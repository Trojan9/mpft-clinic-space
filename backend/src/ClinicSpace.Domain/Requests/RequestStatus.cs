namespace ClinicSpace.Domain.Requests;

/// <summary>
/// Lifecycle of a clinical space request. Submitted is the only state that can change;
/// Accepted and Rejected are final.
/// </summary>
public enum RequestStatus
{
    Submitted = 1,
    Accepted = 2,
    Rejected = 3,
}
