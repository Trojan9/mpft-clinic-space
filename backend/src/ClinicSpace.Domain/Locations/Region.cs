namespace ClinicSpace.Domain.Locations;

/// <summary>Top level of the location hierarchy: Region → Site → Building.</summary>
public class Region
{
    public const int NameMaxLength = 128;

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Site> Sites { get; } = new List<Site>();
}
