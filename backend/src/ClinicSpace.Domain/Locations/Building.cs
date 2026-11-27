namespace ClinicSpace.Domain.Locations;

/// <summary>A building on a site. Requests are made against a building.</summary>
public class Building
{
    public const int NameMaxLength = 160;

    public int Id { get; set; }

    public int SiteId { get; set; }

    public Site? Site { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Inactive buildings stay linked to old requests but cannot be chosen for new ones.</summary>
    public bool IsActive { get; set; } = true;
}
