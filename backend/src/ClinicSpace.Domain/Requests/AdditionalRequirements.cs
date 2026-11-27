namespace ClinicSpace.Domain.Requests;

/// <summary>Facilities the requester needs in the clinical space.</summary>
public sealed record AdditionalRequirements(bool SinkRequired, bool ClinicalCouchRequired, string? Notes)
{
    public const int NotesMaxLength = 1000;
}
