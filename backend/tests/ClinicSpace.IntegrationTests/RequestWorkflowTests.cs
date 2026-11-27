using System.Net;
using System.Text.Json;
using ClinicSpace.Application.Common;
using ClinicSpace.Application.Requests;
using ClinicSpace.Domain.Locations;
using ClinicSpace.Domain.Requests;
using ClinicSpace.Infrastructure.Email;
using ClinicSpace.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicSpace.IntegrationTests;

[Collection(ApiCollection.Name)]
public class RequestWorkflowTests(ClinicSpaceAppFixture app)
{
    [Fact]
    public async Task Requester_submits_a_request_and_sees_it_in_my_requests()
    {
        using var sarah = await app.SignInAsync("sarah.khan");
        var buildingId = await Requests.AnyBuildingIdAsync(app);

        var response = await sarah.PostAsync("/api/requests", Requests.Valid(buildingId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Contain("/api/requests/");
        var created = await ApiSession.ReadAsync<RequestDetailDto>(response);
        created.Status.Should().Be(RequestStatus.Submitted);
        created.Reference.Should().MatchRegex("^CSR-[0-9]{6}$");
        created.CostCode.Should().Be("CC-1234", "cost codes are stored in upper case");
        created.RequesterName.Should().Be("Sarah Khan");
        created.RequesterEmail.Should().Be("sarah.khan@clinicspace.test");
        created.Timeline.Should().ContainSingle(e => e.Type == RequestEventType.Submitted);
        created.OverlappingRequests.Should().BeNull("only administrators see clashes");

        var mine = await sarah.GetJsonAsync<PagedResult<RequestSummaryDto>>("/api/requests/mine?pageSize=100");
        mine.Items.Should().Contain(r => r.Id == created.Id && r.Status == RequestStatus.Submitted);
    }

    [Fact]
    public async Task Requester_name_and_email_always_come_from_the_account_not_the_body()
    {
        using var sarah = await app.SignInAsync("sarah.khan");
        var buildingId = await Requests.AnyBuildingIdAsync(app);
        var body = JsonSerializer.SerializeToElement(Requests.Valid(buildingId), ApiSession.Json);
        var tampered = new Dictionary<string, object?>(body.EnumerateObject().Select(p => new KeyValuePair<string, object?>(p.Name, p.Value)))
        {
            ["requesterName"] = "Somebody Else",
            ["requesterEmail"] = "attacker@evil.test",
        };

        var created = await ApiSession.ReadAsync<RequestDetailDto>(await sarah.PostAsync("/api/requests", tampered));

        created.RequesterName.Should().Be("Sarah Khan");
        created.RequesterEmail.Should().Be("sarah.khan@clinicspace.test");
    }

    [Fact]
    public async Task Invalid_request_returns_field_errors_in_problem_details()
    {
        using var sarah = await app.SignInAsync("sarah.khan");

        var response = await sarah.PostAsync("/api/requests", new { costCode = "bad code!", acceptTerms = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadAsStringAsync();
        problem.Should().Contain("\"costCode\"").And.Contain("\"buildingId\"").And.Contain("\"acceptTerms\"").And.Contain("validation_failed");
    }

    [Fact]
    public async Task Inactive_buildings_cannot_be_requested()
    {
        var inactiveId = await app.WithDbAsync(async db =>
        {
            var site = await db.Sites.FirstAsync();
            var building = new Building { Name = $"Closed {Guid.NewGuid():N}", SiteId = site.Id, IsActive = false };
            db.Buildings.Add(building);
            await db.SaveChangesAsync();
            return building.Id;
        });
        using var sarah = await app.SignInAsync("sarah.khan");

        var response = await sarah.PostAsync("/api/requests", Requests.Valid(inactiveId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Select a building");
    }

    [Fact]
    public async Task Requester_cannot_see_someone_elses_request()
    {
        using var sarah = await app.SignInAsync("sarah.khan");
        using var james = await app.SignInAsync("james.wright");
        var created = await ApiSession.ReadAsync<RequestDetailDto>(await sarah.PostAsync("/api/requests", Requests.Valid(await Requests.AnyBuildingIdAsync(app))));

        (await james.GetAsync($"/api/requests/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var jamesList = await james.GetJsonAsync<PagedResult<RequestSummaryDto>>("/api/requests/mine?pageSize=100");
        jamesList.Items.Should().NotContain(r => r.Id == created.Id);
    }

    [Theory]
    [InlineData("sarah.khan", "/api/requests")]
    [InlineData("sarah.khan", "/api/requests/summary")]
    [InlineData("olivia.bennett", "/api/requests/mine")]
    public async Task Role_policies_are_enforced_on_reads(string username, string url)
    {
        using var session = await app.SignInAsync(username);

        (await session.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrators_cannot_create_requests_and_requesters_cannot_decide()
    {
        using var olivia = await app.SignInAsync("olivia.bennett");
        using var sarah = await app.SignInAsync("sarah.khan");
        var buildingId = await Requests.AnyBuildingIdAsync(app);
        var created = await ApiSession.ReadAsync<RequestDetailDto>(await sarah.PostAsync("/api/requests", Requests.Valid(buildingId)));

        (await olivia.PostAsync("/api/requests", Requests.Valid(buildingId))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await sarah.PostAsync($"/api/requests/{created.Id}/decision", new { decision = "Accept", version = created.Version }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrator_accepts_a_request_the_requester_sees_it_and_an_email_is_sent()
    {
        using var sarah = await app.SignInAsync("sarah.khan");
        using var olivia = await app.SignInAsync("olivia.bennett");
        var created = await ApiSession.ReadAsync<RequestDetailDto>(await sarah.PostAsync("/api/requests", Requests.Valid(await Requests.AnyBuildingIdAsync(app))));

        var forAdmin = await olivia.GetJsonAsync<RequestDetailDto>($"/api/requests/{created.Id}");
        forAdmin.OverlappingRequests.Should().NotBeNull();

        var response = await olivia.PostAsync($"/api/requests/{created.Id}/decision", new { decision = "Accept", notes = "Room 2", version = forAdmin.Version });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var decided = await ApiSession.ReadAsync<RequestDetailDto>(response);
        decided.Status.Should().Be(RequestStatus.Accepted);
        decided.DecidedByName.Should().Be("Olivia Bennett");
        decided.DecisionNotes.Should().Be("Room 2");
        decided.Timeline.Select(e => e.Type).Should().Equal(RequestEventType.Submitted, RequestEventType.Accepted);

        var asRequester = await sarah.GetJsonAsync<RequestDetailDto>($"/api/requests/{created.Id}");
        asRequester.Status.Should().Be(RequestStatus.Accepted);

        var email = await app.WithDbAsync(db => db.EmailMessages.SingleAsync(m =>
            m.RelatedRequestId == created.Id && m.Subject.Contains("accepted")));
        email.ToAddress.Should().Be("sarah.khan@clinicspace.test");
        email.SentAtUtc.Should().BeNull("the background sender has not run yet");

        await using var scope = app.Factory.Services.CreateAsyncScope();
        var sent = await scope.ServiceProvider.GetRequiredService<EmailOutboxDispatcher>().DispatchPendingAsync(CancellationToken.None);
        sent.Should().BeGreaterThan(0);
        var after = await app.WithDbAsync(db => db.EmailMessages.SingleAsync(m => m.Id == email.Id));
        after.SentAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Rejecting_needs_a_reason_and_the_reason_is_stored()
    {
        using var sarah = await app.SignInAsync("sarah.khan");
        using var olivia = await app.SignInAsync("olivia.bennett");
        var created = await ApiSession.ReadAsync<RequestDetailDto>(await sarah.PostAsync("/api/requests", Requests.Valid(await Requests.AnyBuildingIdAsync(app))));

        var noReason = await olivia.PostAsync($"/api/requests/{created.Id}/decision", new { decision = "Reject", version = created.Version });
        noReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var rejected = await olivia.PostAsync($"/api/requests/{created.Id}/decision", new { decision = "Reject", notes = "Building closed", version = created.Version });
        rejected.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApiSession.ReadAsync<RequestDetailDto>(rejected)).DecisionNotes.Should().Be("Building closed");
    }

    [Fact]
    public async Task A_second_decision_on_the_same_request_is_a_conflict()
    {
        using var sarah = await app.SignInAsync("sarah.khan");
        using var olivia = await app.SignInAsync("olivia.bennett");
        using var daniel = await app.SignInAsync("daniel.okafor");
        var created = await ApiSession.ReadAsync<RequestDetailDto>(await sarah.PostAsync("/api/requests", Requests.Valid(await Requests.AnyBuildingIdAsync(app))));

        // Both administrators opened the request at the same version.
        var first = await olivia.PostAsync($"/api/requests/{created.Id}/decision", new { decision = "Accept", version = created.Version });
        var second = await daniel.PostAsync($"/api/requests/{created.Id}/decision", new { decision = "Reject", notes = "No", version = created.Version });

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Even with the latest version, a decided request cannot change.
        var latest = await daniel.GetJsonAsync<RequestDetailDto>($"/api/requests/{created.Id}");
        var third = await daniel.PostAsync($"/api/requests/{created.Id}/decision", new { decision = "Reject", notes = "No", version = latest.Version });
        third.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await third.Content.ReadAsStringAsync()).Should().Contain("already_decided");
    }

    [Fact]
    public async Task Admin_list_filters_by_status_searches_by_reference_and_pages()
    {
        using var sarah = await app.SignInAsync("sarah.khan");
        using var olivia = await app.SignInAsync("olivia.bennett");
        var created = await ApiSession.ReadAsync<RequestDetailDto>(await sarah.PostAsync("/api/requests", Requests.Valid(await Requests.AnyBuildingIdAsync(app), costCode: "ZZ-777")));

        var submitted = await olivia.GetJsonAsync<PagedResult<RequestSummaryDto>>("/api/requests?status=Submitted&pageSize=100");
        submitted.Items.Should().OnlyContain(r => r.Status == RequestStatus.Submitted).And.Contain(r => r.Id == created.Id);

        var byReference = await olivia.GetJsonAsync<PagedResult<RequestSummaryDto>>($"/api/requests?search={created.Reference.ToLowerInvariant()}");
        byReference.Items.Should().ContainSingle().Which.Id.Should().Be(created.Id);

        var byCostCode = await olivia.GetJsonAsync<PagedResult<RequestSummaryDto>>("/api/requests?search=ZZ-777");
        byCostCode.Items.Should().Contain(r => r.Id == created.Id);

        var page = await olivia.GetJsonAsync<PagedResult<RequestSummaryDto>>("/api/requests?pageSize=1&page=2&sort=Reference&descending=false");
        page.Items.Should().HaveCount(1);
        page.PageSize.Should().Be(1);
        page.TotalCount.Should().BeGreaterThan(1);

        var tooBig = await olivia.GetJsonAsync<PagedResult<RequestSummaryDto>>("/api/requests?pageSize=5000");
        tooBig.PageSize.Should().Be(RequestService.MaxPageSize);

        var counts = await olivia.GetJsonAsync<RequestStatusCounts>("/api/requests/summary");
        counts.Submitted.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Admin_detail_lists_overlapping_requests_for_the_same_building()
    {
        using var sarah = await app.SignInAsync("sarah.khan");
        using var james = await app.SignInAsync("james.wright");
        using var olivia = await app.SignInAsync("olivia.bennett");
        var buildingId = await Requests.AnyBuildingIdAsync(app);

        var first = await ApiSession.ReadAsync<RequestDetailDto>(await sarah.PostAsync("/api/requests", Requests.Valid(buildingId, daysAhead: 40, hours: 4)));
        var second = await ApiSession.ReadAsync<RequestDetailDto>(await james.PostAsync("/api/requests", Requests.Valid(buildingId, daysAhead: 40, hours: 2)));

        var detail = await olivia.GetJsonAsync<RequestDetailDto>($"/api/requests/{first.Id}");

        detail.OverlappingRequests.Should().Contain(o => o.Id == second.Id);
    }

    [Fact]
    public async Task Unknown_request_ids_return_404_problem_details()
    {
        using var olivia = await app.SignInAsync("olivia.bennett");

        var response = await olivia.GetAsync($"/api/requests/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("not_found");
    }
}
