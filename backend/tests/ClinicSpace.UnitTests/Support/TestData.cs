using ClinicSpace.Domain.Requests;
using ClinicSpace.Domain.Users;

namespace ClinicSpace.UnitTests.Support;

/// <summary>A clock frozen at a known time, so date rules can be tested exactly.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class TestData
{
    // A Monday in winter (GMT), well away from clock changes.
    public static readonly DateTimeOffset Now = new(2026, 11, 30, 10, 0, 0, TimeSpan.Zero);

    public static User Requester(string name = "Sarah Khan") => new()
    {
        Username = "sarah.khan",
        NormalizedUsername = "SARAH.KHAN",
        DisplayName = name,
        Email = "sarah.khan@clinicspace.test",
        Role = UserRole.Requester,
    };

    public static User Administrator(string name = "Olivia Bennett") => new()
    {
        Username = "olivia.bennett",
        NormalizedUsername = "OLIVIA.BENNETT",
        DisplayName = name,
        Email = "olivia.bennett@clinicspace.test",
        Role = UserRole.Administrator,
    };

    public static ClinicalSpaceRequest SubmittedRequest(User? requester = null, string? notes = null)
    {
        var request = ClinicalSpaceRequest.Submit(
            requester ?? Requester(),
            buildingId: 1,
            costCode: "CC-4510",
            budgetHolderName: "Priya Patel",
            budgetHolderEmail: "priya.patel@clinicspace.test",
            startsAt: Now.AddDays(7),
            endsAt: Now.AddDays(7).AddHours(3),
            new AdditionalRequirements(true, false, notes),
            termsVersion: "2026-11-v1",
            now: Now);
        request.ReferenceNumber = 42;
        return request;
    }
}
