import { describe, expect, it } from "vitest";
import { emptyRequestForm, toPayload, validateRequestForm, type RequestFormValues } from "./requestSchema";

const NOW = new Date("2026-11-30T10:00:00Z");

const valid: RequestFormValues = {
  ...emptyRequestForm,
  costCode: "cc-4510",
  budgetHolderName: "Priya Patel",
  budgetHolderEmail: "priya.patel@nhs.net",
  regionId: "1",
  siteId: "2",
  buildingId: "3",
  startDay: "8",
  startMonth: "12",
  startYear: "2026",
  startHour: "09",
  startMinute: "00",
  endDay: "8",
  endMonth: "12",
  endYear: "2026",
  endHour: "17",
  endMinute: "00",
  sinkRequired: "yes",
  clinicalCouchRequired: "no",
  additionalRequirements: "  Hearing loop  ",
  acceptTerms: true,
};

function errorsFor(values: Partial<RequestFormValues>): Record<string, string | undefined> {
  return validateRequestForm({ ...valid, ...values }, NOW);
}

describe("request form rules", () => {
  it("accepts a complete request", () => {
    expect(validateRequestForm(valid, NOW)).toEqual({});
  });

  it("reports every missing required answer", () => {
    const errors = errorsFor(emptyRequestForm);
    expect(errors).toMatchObject({
      costCode: "Enter a cost code",
      budgetHolderName: "Enter the budget holder's name",
      budgetHolderEmail: "Enter the budget holder's email address",
      regionId: "Select a region",
      siteId: "Select a site",
      buildingId: "Select a building",
      sinkRequired: "Select yes if a sink is required",
      clinicalCouchRequired: "Select yes if a clinical couch is required",
      acceptTerms: "Confirm that you accept the terms and conditions",
      // Date errors appear alongside the others, not only once everything else is fixed.
      startDay: "Enter a start date",
      startHour: "Select a start time",
      endDay: "Enter an end date",
      endHour: "Select an end time",
    });
  });

  it.each([
    [{ costCode: "CC 4510" }, "costCode", "Cost code must only include letters, numbers and hyphens, like CC-1234"],
    [{ budgetHolderEmail: "priya@nhs" }, "budgetHolderEmail", "Enter an email address in the correct format, like name@nhs.net"],
    [{ budgetHolderName: "R2D2" }, "budgetHolderName", "Budget holder name must only include letters, spaces, hyphens and apostrophes"],
    [{ startMonth: "" }, "startDay", "Start date must include a month"],
    [{ startDay: "", startMonth: "" }, "startDay", "Start date must include a day and a month"],
    [{ startDay: "31", startMonth: "11" }, "startDay", "Start date must be a real date"],
    [{ startHour: "" }, "startHour", "Select a start time"],
    [{ startDay: "1", startMonth: "11" }, "startDay", "Start date and time must be in the future"],
    [{ endHour: "08" }, "endDay", "End date and time must be after the start date and time"],
    [{ endHour: "09", endMinute: "15" }, "endDay", "The booking must be at least 30 minutes long"],
    [{ endMonth: "1", endYear: "2027", endDay: "20" }, "endDay", "The booking must be 31 days or shorter"],
    [{ additionalRequirements: "x".repeat(1001) }, "additionalRequirements", "Additional requirements must be 1000 characters or fewer"],
  ])("%o gives the error on %s", (values, field, message) => {
    expect(errorsFor(values as Partial<RequestFormValues>)[field]).toBe(message);
  });

  it("allows names with accents and apostrophes", () => {
    expect(errorsFor({ budgetHolderName: "Seán O’Brien-Zoë" }).budgetHolderName).toBeUndefined();
  });
});

describe("toPayload", () => {
  it("converts UK local times to UTC instants and tidies the answers", () => {
    expect(toPayload(valid)).toEqual({
      costCode: "CC-4510",
      budgetHolderName: "Priya Patel",
      budgetHolderEmail: "priya.patel@nhs.net",
      buildingId: 3,
      startsAt: "2026-12-08T09:00:00.000Z",
      endsAt: "2026-12-08T17:00:00.000Z",
      sinkRequired: true,
      clinicalCouchRequired: false,
      additionalRequirements: "Hearing loop",
      acceptTerms: true,
    });
  });

  it("sends null when there are no other requirements", () => {
    expect(toPayload({ ...valid, additionalRequirements: "   " }).additionalRequirements).toBeNull();
  });
});
