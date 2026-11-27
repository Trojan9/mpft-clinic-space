namespace ClinicSpace.Domain.Common;

/// <summary>
/// Marks an entity whose created and updated timestamps are set automatically when it is saved.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }

    DateTimeOffset UpdatedAtUtc { get; set; }
}
