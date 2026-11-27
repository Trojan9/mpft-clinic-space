namespace ClinicSpace.Application.Requests;

/// <summary>Business rules for requests that may need tuning without a code change.</summary>
public sealed class RequestOptions
{
    public const string SectionName = "Requests";

    /// <summary>Version of the terms and conditions shown on the form. Stored with each request.</summary>
    public string TermsVersion { get; set; } = "2026-11-v1";

    /// <summary>Shortest booking allowed, in minutes.</summary>
    public int MinimumDurationMinutes { get; set; } = 30;

    /// <summary>Longest single booking allowed, in days.</summary>
    public int MaximumDurationDays { get; set; } = 31;

    /// <summary>How far ahead a booking can start, in days.</summary>
    public int MaximumDaysInAdvance { get; set; } = 365;

    /// <summary>
    /// Optional list of email domains allowed for budget holders (for example "nhs.net", "mpft.nhs.uk").
    /// Empty means any domain is accepted.
    /// </summary>
    public IList<string> AllowedBudgetHolderEmailDomains { get; set; } = new List<string>();
}
