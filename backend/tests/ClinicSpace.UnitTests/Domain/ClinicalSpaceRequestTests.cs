using ClinicSpace.Domain.Common;
using ClinicSpace.Domain.Requests;
using ClinicSpace.UnitTests.Support;

namespace ClinicSpace.UnitTests.Domain;

public class ClinicalSpaceRequestTests
{
    [Fact]
    public void Submit_creates_a_submitted_request_with_requester_snapshot_and_audit_event()
    {
        var requester = TestData.Requester();

        var request = TestData.SubmittedRequest(requester);

        request.Status.Should().Be(RequestStatus.Submitted);
        request.RequesterId.Should().Be(requester.Id);
        request.RequesterName.Should().Be("Sarah Khan");
        request.RequesterEmail.Should().Be("sarah.khan@clinicspace.test");
        request.TermsAcceptedAtUtc.Should().Be(TestData.Now);
        request.Events.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Type = RequestEventType.Submitted, ActorUserId = requester.Id, OccurredAtUtc = TestData.Now });
    }

    [Fact]
    public void Submit_refuses_an_end_time_that_is_not_after_the_start()
    {
        var act = () => ClinicalSpaceRequest.Submit(
            TestData.Requester(), 1, "CC-1", "Priya Patel", "p@x.test",
            TestData.Now.AddDays(1), TestData.Now.AddDays(1), new AdditionalRequirements(false, false, null), "v1", TestData.Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("invalid_period");
    }

    [Fact]
    public void Accept_records_the_decision_and_an_audit_event()
    {
        var request = TestData.SubmittedRequest();
        var admin = TestData.Administrator();
        var decidedAt = TestData.Now.AddHours(2);

        request.Accept(admin, "  Room 2  ", decidedAt);

        request.Status.Should().Be(RequestStatus.Accepted);
        request.DecidedById.Should().Be(admin.Id);
        request.DecidedAtUtc.Should().Be(decidedAt);
        request.DecisionNotes.Should().Be("Room 2");
        request.Events.Last().Should().BeEquivalentTo(new { Type = RequestEventType.Accepted, ActorName = "Olivia Bennett", Notes = "Room 2" });
    }

    [Fact]
    public void Accept_without_notes_stores_no_notes()
    {
        var request = TestData.SubmittedRequest();

        request.Accept(TestData.Administrator(), "   ", TestData.Now);

        request.DecisionNotes.Should().BeNull();
    }

    [Fact]
    public void Reject_requires_a_reason()
    {
        var request = TestData.SubmittedRequest();

        var act = () => request.Reject(TestData.Administrator(), " ", TestData.Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("reason_required");
        request.Status.Should().Be(RequestStatus.Submitted);
    }

    [Fact]
    public void Reject_records_the_reason()
    {
        var request = TestData.SubmittedRequest();

        request.Reject(TestData.Administrator(), "Building closed", TestData.Now);

        request.Status.Should().Be(RequestStatus.Rejected);
        request.DecisionNotes.Should().Be("Building closed");
        request.Events.Last().Type.Should().Be(RequestEventType.Rejected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_decided_request_cannot_be_decided_again(bool firstAccepted)
    {
        var request = TestData.SubmittedRequest();
        var admin = TestData.Administrator();
        if (firstAccepted)
        {
            request.Accept(admin, null, TestData.Now);
        }
        else
        {
            request.Reject(admin, "No", TestData.Now);
        }

        var accept = () => request.Accept(admin, null, TestData.Now);
        var reject = () => request.Reject(admin, "Changed my mind", TestData.Now);

        accept.Should().Throw<DomainException>().Which.Code.Should().Be("already_decided");
        reject.Should().Throw<DomainException>().Which.Code.Should().Be("already_decided");
        request.Events.Should().HaveCount(2);
    }

    [Fact]
    public void Only_an_administrator_can_decide()
    {
        var request = TestData.SubmittedRequest();

        var act = () => request.Accept(TestData.Requester("Someone Else"), null, TestData.Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("not_administrator");
    }

    [Fact]
    public void An_administrator_cannot_decide_their_own_request()
    {
        var admin = TestData.Administrator();
        var request = TestData.SubmittedRequest(admin);

        var act = () => request.Accept(admin, null, TestData.Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("own_request");
    }

    [Theory]
    [InlineData(1, "CSR-000001")]
    [InlineData(123, "CSR-000123")]
    [InlineData(1234567, "CSR-1234567")]
    public void FormatReference_pads_to_six_digits(int number, string expected) =>
        ClinicalSpaceRequest.FormatReference(number).Should().Be(expected);

    [Theory]
    [InlineData("CSR-000123", 123)]
    [InlineData("csr-123", 123)]
    [InlineData("  123 ", 123)]
    public void TryParseReference_accepts_common_forms(string input, int expected)
    {
        ClinicalSpaceRequest.TryParseReference(input, out var number).Should().BeTrue();
        number.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("CSR-")]
    [InlineData("CSR-abc")]
    [InlineData("-5")]
    [InlineData("0")]
    [InlineData("Harplands")]
    public void TryParseReference_rejects_anything_else(string? input) =>
        ClinicalSpaceRequest.TryParseReference(input, out _).Should().BeFalse();
}
