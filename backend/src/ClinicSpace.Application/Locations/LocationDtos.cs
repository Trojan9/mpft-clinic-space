namespace ClinicSpace.Application.Locations;

/// <summary>An item in a Region, Site or Building drop-down.</summary>
public sealed record LocationOption(int Id, string Name);

/// <summary>Full location path of a building, for display.</summary>
public sealed record LocationPath(int RegionId, string Region, int SiteId, string Site, int BuildingId, string Building);
