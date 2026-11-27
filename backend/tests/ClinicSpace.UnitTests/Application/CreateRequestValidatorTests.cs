using ClinicSpace.Application.Requests;
using ClinicSpace.UnitTests.Support;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;

namespace ClinicSpace.UnitTests.Application;

public class CreateRequestValidatorTests
{
    private static CreateRequestValidator Validator(RequestOptions? options = null) =>
        new(new FixedTimeProvider(TestData.Now), Options.Create(options ?? new RequestOptions()));

    private static CreateRequestCommand Valid() => new()
    {
        CostCode = "CC-4510",
        BudgetHolderName = "Priya O'Neil-Patel",
        BudgetHolderEmail = "priya.patel@nhs.net",
        BuildingId = 3,
        StartsAt = TestData.Now.AddDays(2),
        EndsAt = TestData.Now.AddDays(2).AddHours(4),
        SinkRequired = true,
        ClinicalCouchRequired = false,
        AdditionalRequirements = "Hearing loop",
        AcceptTerms = true,
    };

    [Fact]
    public void A_complete_request_is_valid() =>
        Validator().TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void An_empty_request_reports_every_required_field()
    {
        var result = Validator().TestValidate(new CreateRequestCommand());

        result.ShouldHaveValidationErrorFor(x => x.CostCode).WithErrorMessage("Enter a cost code");
        result.ShouldHaveValidationErrorFor(x => x.BudgetHolderName).WithErrorMessage("Enter the budget holder's name");
        result.ShouldHaveValidationErrorFor(x => x.BudgetHolderEmail).WithErrorMessage("Enter the budget holder's email address");
        result.ShouldHaveValidationErrorFor(x => x.BuildingId).WithErrorMessage("Select a building");
        result.ShouldHaveValidationErrorFor(x => x.StartsAt).WithErrorMessage("Enter a start date and time");
        result.ShouldHaveValidationErrorFor(x => x.EndsAt).WithErrorMessage("Enter an end date and time");
        result.ShouldHaveValidationErrorFor(x => x.SinkRequired).WithErrorMessage("Select yes if a sink is required");
        result.ShouldHaveValidationErrorFor(x => x.ClinicalCouchRequired).WithErrorMessage("Select yes if a clinical couch is required");
        result.ShouldHaveValidationErrorFor(x => x.AcceptTerms).WithErrorMessage("Confirm that you accept the terms and conditions");
    }

    [Theory]
    [InlineData("CC 4510")]
    [InlineData("CC--4510")]
    [InlineData("-4510")]
    [InlineData("CC_4510")]
    [InlineData("4510;DROP")]
    public void Cost_code_must_be_letters_numbers_and_single_hyphens(string costCode) =>
        Validator().TestValidate(Valid() with { CostCode = costCode }).ShouldHaveValidationErrorFor(x => x.CostCode);

    [Fact]
    public void Cost_code_has_a_maximum_length() =>
        Validator().TestValidate(Valid() with { CostCode = new string('A', 21) })
            .ShouldHaveValidationErrorFor(x => x.CostCode).WithErrorMessage("Cost code must be 20 characters or fewer");

    [Theory]
    [InlineData("Zoë Brontë")]
    [InlineData("Seán O’Brien")]
    [InlineData("Dr. Ada Lovelace")]
    public void Budget_holder_names_allow_real_world_characters(string name) =>
        Validator().TestValidate(Valid() with { BudgetHolderName = name }).ShouldNotHaveValidationErrorFor(x => x.BudgetHolderName);

    [Theory]
    [InlineData("<script>")]
    [InlineData("Priya123")]
    [InlineData("-Priya")]
    public void Budget_holder_name_rejects_other_characters(string name) =>
        Validator().TestValidate(Valid() with { BudgetHolderName = name }).ShouldHaveValidationErrorFor(x => x.BudgetHolderName);

    [Theory]
    [InlineData("priya")]
    [InlineData("priya@nhs")]
    [InlineData("priya patel@nhs.net")]
    [InlineData("@nhs.net")]
    public void Budget_holder_email_must_look_like_an_email(string email) =>
        Validator().TestValidate(Valid() with { BudgetHolderEmail = email })
            .ShouldHaveValidationErrorFor(x => x.BudgetHolderEmail)
            .WithErrorMessage("Enter an email address in the correct format, like name@nhs.net");

    [Theory]
    [InlineData("priya@nhs.net", true)]
    [InlineData("priya@mpft.nhs.uk", true)]
    [InlineData("priya@NHS.NET", true)]
    [InlineData("priya@gmail.com", false)]
    [InlineData("priya@evilnhs.net", false)]
    public void Budget_holder_email_domain_can_be_restricted(string email, bool allowed)
    {
        var options = new RequestOptions { AllowedBudgetHolderEmailDomains = ["nhs.net", "nhs.uk"] };
        var result = Validator(options).TestValidate(Valid() with { BudgetHolderEmail = email });

        if (allowed)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.BudgetHolderEmail);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.BudgetHolderEmail).WithErrorMessage("Budget holder email address must end with @nhs.net or @nhs.uk");
        }
    }

    [Fact]
    public void Start_must_be_in_the_future() =>
        Validator().TestValidate(Valid() with { StartsAt = TestData.Now.AddHours(-1), EndsAt = TestData.Now.AddHours(2) })
            .ShouldHaveValidationErrorFor(x => x.StartsAt).WithErrorMessage("Start date and time must be in the future");

    [Fact]
    public void A_few_minutes_of_clock_difference_is_tolerated() =>
        Validator().TestValidate(Valid() with { StartsAt = TestData.Now.AddMinutes(-2), EndsAt = TestData.Now.AddHours(2) })
            .ShouldNotHaveValidationErrorFor(x => x.StartsAt);

    [Fact]
    public void Start_must_be_within_the_booking_window() =>
        Validator().TestValidate(Valid() with { StartsAt = TestData.Now.AddDays(366), EndsAt = TestData.Now.AddDays(366).AddHours(1) })
            .ShouldHaveValidationErrorFor(x => x.StartsAt).WithErrorMessage("Start date must be within the next 365 days");

    [Fact]
    public void End_must_be_after_start()
    {
        var start = TestData.Now.AddDays(1);
        Validator().TestValidate(Valid() with { StartsAt = start, EndsAt = start.AddHours(-1) })
            .ShouldHaveValidationErrorFor(x => x.EndsAt).WithErrorMessage("End date and time must be after the start date and time");
    }

    [Fact]
    public void Booking_has_a_minimum_length()
    {
        var start = TestData.Now.AddDays(1);
        Validator().TestValidate(Valid() with { StartsAt = start, EndsAt = start.AddMinutes(15) })
            .ShouldHaveValidationErrorFor(x => x.EndsAt).WithErrorMessage("The booking must be at least 30 minutes long");
    }

    [Fact]
    public void Booking_has_a_maximum_length()
    {
        var start = TestData.Now.AddDays(1);
        Validator().TestValidate(Valid() with { StartsAt = start, EndsAt = start.AddDays(32) })
            .ShouldHaveValidationErrorFor(x => x.EndsAt).WithErrorMessage("The booking must be 31 days or shorter");
    }

    [Fact]
    public void Additional_requirements_are_optional_but_limited()
    {
        Validator().TestValidate(Valid() with { AdditionalRequirements = null }).ShouldNotHaveValidationErrorFor(x => x.AdditionalRequirements);
        Validator().TestValidate(Valid() with { AdditionalRequirements = new string('a', 1001) })
            .ShouldHaveValidationErrorFor(x => x.AdditionalRequirements);
    }

    [Fact]
    public void Terms_must_be_accepted() =>
        Validator().TestValidate(Valid() with { AcceptTerms = false })
            .ShouldHaveValidationErrorFor(x => x.AcceptTerms);
}

public class DecideRequestValidatorTests
{
    private readonly DecideRequestValidator _validator = new();

    [Fact]
    public void Accept_without_notes_is_valid() =>
        _validator.TestValidate(new DecideRequestCommand { Decision = Decision.Accept, Version = "AAAAAAAAB9E=" })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Reject_needs_a_reason() =>
        _validator.TestValidate(new DecideRequestCommand { Decision = Decision.Reject, Notes = "  ", Version = "AAAAAAAAB9E=" })
            .ShouldHaveValidationErrorFor(x => x.Notes).WithErrorMessage("Enter a reason for rejecting the request");

    [Fact]
    public void Decision_and_version_are_required()
    {
        var result = _validator.TestValidate(new DecideRequestCommand());
        result.ShouldHaveValidationErrorFor(x => x.Decision);
        result.ShouldHaveValidationErrorFor(x => x.Version);
    }

    [Fact]
    public void Unknown_decision_values_are_rejected() =>
        _validator.TestValidate(new DecideRequestCommand { Decision = (Decision)99, Version = "x" })
            .ShouldHaveValidationErrorFor(x => x.Decision);
}
