using System.Text.RegularExpressions;
using ClinicSpace.Domain.Requests;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Application.Requests;

/// <summary>
/// Server-side validation for a new request. The web app mirrors these rules (Zod) for quick
/// feedback, but this validator is the one that counts. Messages follow the NHS service manual
/// error message style: say what went wrong and how to fix it.
/// </summary>
public sealed partial class CreateRequestValidator : AbstractValidator<CreateRequestCommand>
{
    public CreateRequestValidator(TimeProvider clock, IOptions<RequestOptions> options)
    {
        var rules = options.Value;

        RuleFor(x => x.CostCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Enter a cost code")
            .MaximumLength(ClinicalSpaceRequest.CostCodeMaxLength).WithMessage($"Cost code must be {ClinicalSpaceRequest.CostCodeMaxLength} characters or fewer")
            .Must(v => CostCodePattern().IsMatch(v!.Trim())).WithMessage("Cost code must only include letters, numbers and hyphens, like CC-1234");

        RuleFor(x => x.BudgetHolderName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Enter the budget holder's name")
            .MaximumLength(ClinicalSpaceRequest.BudgetHolderNameMaxLength).WithMessage($"Budget holder name must be {ClinicalSpaceRequest.BudgetHolderNameMaxLength} characters or fewer")
            .Must(v => NamePattern().IsMatch(v!.Trim())).WithMessage("Budget holder name must only include letters, spaces, hyphens and apostrophes");

        RuleFor(x => x.BudgetHolderEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Enter the budget holder's email address")
            .MaximumLength(254).WithMessage("Budget holder email address must be 254 characters or fewer")
            .Must(v => EmailPattern().IsMatch(v!.Trim())).WithMessage("Enter an email address in the correct format, like name@nhs.net")
            .Must(v => IsAllowedDomain(v!, rules.AllowedBudgetHolderEmailDomains))
            .WithMessage($"Budget holder email address must end with {string.Join(" or ", rules.AllowedBudgetHolderEmailDomains.Select(d => "@" + d))}");

        RuleFor(x => x.BuildingId)
            .NotNull().WithMessage("Select a building")
            .GreaterThan(0).WithMessage("Select a building");

        RuleFor(x => x.StartsAt)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Enter a start date and time")
            // Allow five minutes of clock difference between browser and server.
            .Must(v => v!.Value > clock.GetUtcNow().AddMinutes(-5)).WithMessage("Start date and time must be in the future")
            .Must(v => v!.Value <= clock.GetUtcNow().AddDays(rules.MaximumDaysInAdvance))
            .WithMessage($"Start date must be within the next {rules.MaximumDaysInAdvance} days");

        RuleFor(x => x.EndsAt)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Enter an end date and time")
            .Must((cmd, end) => cmd.StartsAt is null || end > cmd.StartsAt).WithMessage("End date and time must be after the start date and time")
            .Must((cmd, end) => cmd.StartsAt is null || end!.Value - cmd.StartsAt.Value >= TimeSpan.FromMinutes(rules.MinimumDurationMinutes))
            .WithMessage($"The booking must be at least {rules.MinimumDurationMinutes} minutes long")
            .Must((cmd, end) => cmd.StartsAt is null || end!.Value - cmd.StartsAt.Value <= TimeSpan.FromDays(rules.MaximumDurationDays))
            .WithMessage($"The booking must be {rules.MaximumDurationDays} days or shorter");

        RuleFor(x => x.SinkRequired)
            .NotNull().WithMessage("Select yes if a sink is required");

        RuleFor(x => x.ClinicalCouchRequired)
            .NotNull().WithMessage("Select yes if a clinical couch is required");

        RuleFor(x => x.AdditionalRequirements)
            .MaximumLength(AdditionalRequirements.NotesMaxLength)
            .WithMessage($"Additional requirements must be {AdditionalRequirements.NotesMaxLength} characters or fewer");

        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("Confirm that you accept the terms and conditions");
    }

    internal static bool IsAllowedDomain(string email, IList<string> allowedDomains)
    {
        if (allowedDomains.Count == 0)
        {
            return true;
        }

        var at = email.LastIndexOf('@');
        if (at < 0)
        {
            return false;
        }

        var domain = email[(at + 1)..].Trim();
        return allowedDomains.Any(d =>
            domain.Equals(d, StringComparison.OrdinalIgnoreCase)
            || domain.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
    }

    // Letters, digits and single hyphens, for example "CC-1234" or "4510ABC".
    [GeneratedRegex(@"^[A-Za-z0-9]+(-[A-Za-z0-9]+)*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex CostCodePattern();

    // Unicode letters (O'Brien, Zoë, Smith-Jones), spaces, hyphens, apostrophes and full stops.
    [GeneratedRegex(@"^[\p{L}][\p{L}\p{M} .'’-]*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex NamePattern();

    // Pragmatic check: one @, no spaces, a dot in the domain. Full RFC 5322 checks are not useful here.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex EmailPattern();
}

public sealed class DecideRequestValidator : AbstractValidator<DecideRequestCommand>
{
    public DecideRequestValidator()
    {
        RuleFor(x => x.Decision)
            .NotNull().WithMessage("Select whether to accept or reject the request")
            .IsInEnum().WithMessage("Select whether to accept or reject the request");

        RuleFor(x => x.Notes)
            .NotEmpty().When(x => x.Decision == Decision.Reject)
            .WithMessage("Enter a reason for rejecting the request");

        RuleFor(x => x.Notes)
            .MaximumLength(ClinicalSpaceRequest.DecisionNotesMaxLength)
            .WithMessage($"Notes must be {ClinicalSpaceRequest.DecisionNotesMaxLength} characters or fewer");

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("The request version is missing. Reload the page and try again.");
    }
}
