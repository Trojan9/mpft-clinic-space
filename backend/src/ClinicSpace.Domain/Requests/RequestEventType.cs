namespace ClinicSpace.Domain.Requests;

/// <summary>Kinds of entry in a request's audit trail.</summary>
public enum RequestEventType
{
    Submitted = 1,
    Accepted = 2,
    Rejected = 3,
}
