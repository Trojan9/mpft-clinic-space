using ClinicSpace.Application.Notifications;
using ClinicSpace.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ClinicSpace.UnitTests.Application;

public class RequestEmailFactoryTests
{
    private const string Location = "Main Building, St George's Hospital (Stafford)";

    private static RequestEmailFactory Factory() =>
        new(Options.Create(new NotificationOptions { PublicBaseUrl = "https://clinicspace.example/" }), new FixedTimeProvider(TestData.Now));

    [Fact]
    public void Accepted_email_goes_to_the_requester_with_reference_and_link()
    {
        var request = TestData.SubmittedRequest();
        request.Accept(TestData.Administrator(), "Room 2", TestData.Now);

        var email = Factory().Decided(request, Location);

        email.ToAddress.Should().Be("sarah.khan@clinicspace.test");
        email.ToName.Should().Be("Sarah Khan");
        email.Subject.Should().Be("Your clinical space request CSR-000042 has been accepted");
        email.RelatedRequestId.Should().Be(request.Id);
        email.NextAttemptAtUtc.Should().Be(TestData.Now);
        email.TextBody.Should().Contain("Notes from the administrator: Room 2")
            .And.Contain("Reference: CSR-000042")
            .And.Contain($"https://clinicspace.example/requests/{request.Id}");
        email.HtmlBody.Should().Contain("Request accepted");
    }

    [Fact]
    public void Rejected_email_includes_the_reason()
    {
        var request = TestData.SubmittedRequest();
        request.Reject(TestData.Administrator(), "Closed for maintenance", TestData.Now);

        var email = Factory().Decided(request, Location);

        email.Subject.Should().Be("Your clinical space request CSR-000042 has been rejected");
        email.TextBody.Should().Contain("Reason: Closed for maintenance");
    }

    [Fact]
    public void User_input_is_html_encoded_so_it_cannot_inject_markup()
    {
        var request = TestData.SubmittedRequest(notes: "<script>alert('x')</script><a href=\"https://evil.test\">click</a>");
        request.Reject(TestData.Administrator(), "<img src=x onerror=alert(1)>", TestData.Now);

        var email = Factory().Decided(request, Location);

        email.HtmlBody.Should().NotContain("<script>").And.NotContain("<img").And.NotContain("href=\"https://evil.test\"");
        email.HtmlBody.Should().Contain("&lt;script&gt;").And.Contain("&lt;img src=x onerror=alert(1)&gt;");
    }

    [Fact]
    public void Submission_email_confirms_receipt()
    {
        var email = Factory().Submitted(TestData.SubmittedRequest(), Location);

        email.Subject.Should().Be("We have received your clinical space request CSR-000042");
        email.TextBody.Should().Contain("An administrator will review it");
    }

    [Fact]
    public void A_submitted_request_cannot_produce_a_decision_email()
    {
        var act = () => Factory().Decided(TestData.SubmittedRequest(), Location);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Facilities_are_described_in_plain_english() =>
        RequestEmailFactory.DescribeFacilities(TestData.SubmittedRequest(notes: "Hearing loop"))
            .Should().Be("Sink required; No clinical couch required; Other: Hearing loop");
}
