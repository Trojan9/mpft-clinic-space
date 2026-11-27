import { useEffect, useMemo } from "react";
import { useForm, type FieldErrors } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { locationsApi } from "../../api/endpoints";
import { useCurrentUser } from "../../auth/AuthContext";
import { BackLink } from "../../components/BackLink";
import { Button } from "../../components/Button";
import { Details, InsetText, ProblemPanel } from "../../components/Callouts";
import { ErrorSummary, type ErrorSummaryItem } from "../../components/ErrorSummary";
import { Checkbox, DateTimeInput, Radios, Select, TextInput, Textarea } from "../../components/FormFields";
import { Page } from "../../components/Page";
import { SummaryList } from "../../components/SummaryList";
import { useRequestDraft } from "./RequestDraft";
import { emptyRequestForm, FIELD_ORDER, requestFormResolver, RULES, type RequestFormValues } from "./requestSchema";
import { TermsAndConditions } from "./TermsAndConditions";

const YES_NO = [
  { value: "yes", label: "Yes" },
  { value: "no", label: "No" },
];

export function summaryFromErrors(errors: FieldErrors<RequestFormValues>): ErrorSummaryItem[] {
  return FIELD_ORDER.filter((field) => errors[field]?.message).map((field) => ({
    fieldId: field,
    message: String(errors[field]!.message),
  }));
}

export function RequestFormPage() {
  const user = useCurrentUser();
  const navigate = useNavigate();
  const { draft, setDraft, serverErrors, setServerErrors } = useRequestDraft();
  const resolver = useMemo(() => requestFormResolver(), []);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    getValues,
    setError,
    formState: { errors, submitCount },
  } = useForm<RequestFormValues>({
    resolver,
    defaultValues: draft?.values ?? emptyRequestForm,
    shouldFocusError: false, // The error summary takes focus instead.
  });

  // Errors the API returned on submit are shown against the matching fields.
  useEffect(() => {
    for (const [field, message] of Object.entries(serverErrors)) {
      setError(field as keyof RequestFormValues, { type: "server", message });
    }
  }, [serverErrors, setError]);

  const regionId = watch("regionId");
  const siteId = watch("siteId");
  const notes = watch("additionalRequirements") ?? "";

  const regions = useQuery({ queryKey: ["locations", "regions"], queryFn: locationsApi.regions, staleTime: 5 * 60_000 });
  const sites = useQuery({
    queryKey: ["locations", "sites", regionId],
    queryFn: () => locationsApi.sites(Number(regionId)),
    enabled: Boolean(regionId),
    staleTime: 5 * 60_000,
  });
  const buildings = useQuery({
    queryKey: ["locations", "buildings", siteId],
    queryFn: () => locationsApi.buildings(Number(siteId)),
    enabled: Boolean(siteId),
    staleTime: 5 * 60_000,
  });

  // A <select> drops a value whose option has not loaded yet. When returning from the check answers
  // page, re-apply the saved choice once each list arrives.
  useEffect(() => {
    if (regions.data) setValue("regionId", getValues("regionId"));
  }, [regions.data, setValue, getValues]);
  useEffect(() => {
    if (sites.data) setValue("siteId", getValues("siteId"));
  }, [sites.data, setValue, getValues]);
  useEffect(() => {
    if (buildings.data) setValue("buildingId", getValues("buildingId"));
  }, [buildings.data, setValue, getValues]);

  const toOptions = (items?: { id: number; name: string }[]) =>
    (items ?? []).map((item) => ({ value: String(item.id), label: item.name }));

  const onSubmit = handleSubmit((values) => {
    const label = (items: { id: number; name: string }[] | undefined, id: string) =>
      items?.find((item) => String(item.id) === id)?.name ?? "";
    setServerErrors({});
    setDraft({
      values,
      labels: {
        region: label(regions.data, values.regionId),
        site: label(sites.data, values.siteId),
        building: label(buildings.data, values.buildingId),
      },
    });
    navigate("/requests/new/check");
  });

  const summary = summaryFromErrors(errors);
  const showSummary = (submitCount > 0 || Object.keys(serverErrors).length > 0) && summary.length > 0;
  const locationsFailed = regions.isError || sites.isError || buildings.isError;

  return (
    <Page
      title="Request a clinical space"
      caption="New request"
      hasError={showSummary}
      before={
        <>
          <BackLink to="/requests">Back to my requests</BackLink>
          {showSummary ? <ErrorSummary errors={summary} /> : null}
        </>
      }
    >
      <p className="mb-8 max-w-2xl">
        Tell us where and when you need a clinical space. You can check your answers before you submit.
      </p>

      <form noValidate onSubmit={onSubmit} aria-label="Request a clinical space">
        <section className="nhs-card" aria-labelledby="your-details">
          <h2 id="your-details" className="nhs-heading-m">
            Your details
          </h2>
          <p className="nhs-hint">These come from your account. Contact the service desk if they are wrong.</p>
          <SummaryList
            className="mb-0"
            rows={[
              { key: "Name", value: <span data-testid="requester-name">{user.displayName}</span> },
              { key: "Email address", value: <span data-testid="requester-email">{user.email}</span> },
            ]}
          />
        </section>

        <section className="nhs-card" aria-labelledby="budget">
          <h2 id="budget" className="nhs-heading-m">
            Budget
          </h2>
          <TextInput
            id="costCode"
            label="Cost code"
            hint="The cost code that will pay for the space, like CC-4510"
            widthClass="max-w-xs"
            autoComplete="off"
            spellCheck={false}
            error={errors.costCode?.message}
            {...register("costCode")}
          />
          <TextInput
            id="budgetHolderName"
            label="Budget holder name"
            autoComplete="off"
            spellCheck={false}
            error={errors.budgetHolderName?.message}
            {...register("budgetHolderName")}
          />
          <TextInput
            id="budgetHolderEmail"
            label="Budget holder email address"
            type="email"
            autoComplete="off"
            spellCheck={false}
            error={errors.budgetHolderEmail?.message}
            {...register("budgetHolderEmail")}
          />
        </section>

        <section className="nhs-card" aria-labelledby="location">
          <h2 id="location" className="nhs-heading-m">
            Location
          </h2>
          {locationsFailed ? (
            <ProblemPanel message="We could not load the list of locations. Refresh the page to try again." />
          ) : null}
          <Select
            id="regionId"
            label="Region"
            placeholder={regions.isLoading ? "Loading regions…" : "Select a region"}
            options={toOptions(regions.data)}
            error={errors.regionId?.message}
            {...register("regionId", {
              onChange: () => {
                setValue("siteId", "");
                setValue("buildingId", "");
              },
            })}
          />
          <Select
            id="siteId"
            label="Site"
            hint={!regionId ? "Select a region first" : undefined}
            placeholder={sites.isLoading ? "Loading sites…" : "Select a site"}
            options={toOptions(sites.data)}
            disabled={!regionId}
            error={errors.siteId?.message}
            {...register("siteId", { onChange: () => setValue("buildingId", "") })}
          />
          <Select
            id="buildingId"
            label="Building"
            hint={!siteId ? "Select a site first" : undefined}
            placeholder={buildings.isLoading ? "Loading buildings…" : "Select a building"}
            options={toOptions(buildings.data)}
            disabled={!siteId}
            error={errors.buildingId?.message}
            {...register("buildingId")}
          />
        </section>

        <section className="nhs-card" aria-labelledby="when">
          <h2 id="when" className="nhs-heading-m">
            Date and time
          </h2>
          <InsetText>
            <p className="m-0">
              Times are UK time. A booking must be between {RULES.minimumDurationMinutes} minutes and{" "}
              {RULES.maximumDurationDays} days long.
            </p>
          </InsetText>
          <DateTimeInput
            id="start"
            legend="Start date and time"
            hint="For example, 8 12 2026 and 09 : 00"
            dateError={errors.startDay?.message}
            timeError={errors.startHour?.message}
            register={(part) => register(`start${part}`)}
          />
          <DateTimeInput
            id="end"
            legend="End date and time"
            hint="For example, 8 12 2026 and 17 : 00"
            dateError={errors.endDay?.message}
            timeError={errors.endHour?.message}
            register={(part) => register(`end${part}`)}
          />
        </section>

        <section className="nhs-card" aria-labelledby="requirements">
          <h2 id="requirements" className="nhs-heading-m">
            Requirements
          </h2>
          <Radios
            id="sinkRequired"
            legend="Is a sink required?"
            inline
            options={YES_NO}
            error={errors.sinkRequired?.message}
            inputProps={register("sinkRequired")}
          />
          <Radios
            id="clinicalCouchRequired"
            legend="Is a clinical couch required?"
            inline
            options={YES_NO}
            error={errors.clinicalCouchRequired?.message}
            inputProps={register("clinicalCouchRequired")}
          />
          <Textarea
            id="additionalRequirements"
            label="Other requirements (optional)"
            hint="For example, wheelchair access, a hearing loop or space for a family member"
            maxChars={RULES.notesMax}
            currentLength={notes.length}
            error={errors.additionalRequirements?.message}
            {...register("additionalRequirements")}
          />
        </section>

        <section className="nhs-card" aria-labelledby="terms">
          <h2 id="terms" className="nhs-heading-m">
            Terms and conditions
          </h2>
          <Details summary="Read the terms and conditions">
            <TermsAndConditions />
          </Details>
          <Checkbox
            id="acceptTerms"
            label="I have read and accept the terms and conditions"
            error={errors.acceptTerms?.message}
            {...register("acceptTerms")}
          />
        </section>

        <Button type="submit">Continue</Button>
      </form>
    </Page>
  );
}
