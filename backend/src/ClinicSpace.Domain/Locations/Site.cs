namespace ClinicSpace.Domain.Locations;

/// <summary>A site (for example a hospital or health centre) within a region.</summary>
public class Site
{
    public const int NameMaxLength = 160;

    public int Id { get; set; }

    public int RegionId { get; set; }

    public Region? Region { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Building> Buildings { get; } = new List<Building>();
}
