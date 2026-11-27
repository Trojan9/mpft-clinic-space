import { z } from "zod";
import type { Resolver } from "react-hook-form";
import type { CreateRequestPayload } from "../../api/types";
import { isRealDate, ukLocalToDate } from "../../lib/dates";

/**
 * Client-side rules for a new request. They mirror the API's FluentValidation rules so people get
 * instant feedback, but the API re-checks everything: this is a convenience, not a control.
 */
export const RULES = {
  costCodeMax: 20,
  nameMax: 128,
  emailMax: 254,
  notesMax: 1000,
  minimumDurationMinutes: 30,
  maximumDurationDays: 31,
  maximumDaysInAdvance: 365,
} as const;

const costCodePattern = /^[A-Za-z0-9]+(-[A-Za-z0-9]+)*$/;
const namePattern = /^\p{L}[\p{L}\p{M} .'’-]*$/u;
const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

const yesNo = (message: string) =>
  z.enum(["yes", "no"], { errorMap: () => ({ message }) });

export const requestFieldSchema = z.object({
  costCode: z
    .string()
    .trim()
    .min(1, "Enter a cost code")
    .max(RULES.costCodeMax, `Cost code must be ${RULES.costCodeMax} characters or fewer`)
    .regex(costCodePattern, "Cost code must only include letters, numbers and hyphens, like CC-1234"),
  budgetHolderName: z
    .string()
    .trim()
    .min(1, "Enter the budget holder's name")
    .max(RULES.nameMax, `Budget holder name must be ${RULES.nameMax} characters or fewer`)
    .regex(namePattern, "Budget holder name must only include letters, spaces, hyphens and apostrophes"),
  budgetHolderEmail: z
    .string()
    .trim()
    .min(1, "Enter the budget holder's email address")
    .max(RULES.emailMax, `Budget holder email address must be ${RULES.emailMax} characters or fewer`)
    .regex(emailPattern, "Enter an email address in the correct format, like name@nhs.net"),
  regionId: z.string().min(1, "Select a region"),
  siteId: z.string().min(1, "Select a site"),
  buildingId: z.string().min(1, "Select a building"),
  startDay: z.string(),
  startMonth: z.string(),
  startYear: z.string(),
  startHour: z.string(),
  startMinute: z.string(),
  endDay: z.string(),
  endMonth: z.string(),
  endYear: z.string(),
  endHour: z.string(),
  endMinute: z.string(),
  sinkRequired: yesNo("Select yes if a sink is required"),
  clinicalCouchRequired: yesNo("Select yes if a clinical couch is required"),
  additionalRequirements: z
    .string()
    .max(RULES.notesMax, `Additional requirements must be ${RULES.notesMax} characters or fewer`),
  acceptTerms: z.literal(true, { errorMap: () => ({ message: "Confirm that you accept the terms and conditions" }) }),
});

export type RequestFormValues = z.infer<typeof requestFieldSchema>;

type Prefix = "start" | "end";

interface ParsedDateTime {
  date?: Date;
  dateError?: string;
  timeError?: string;
}

/** Checks one date-and-time group and returns either the instant or the error messages. */
export function parseDateTime(values: Pick<RequestFormValues, `${Prefix}${"Day" | "Month" | "Year" | "Hour" | "Minute"}`>, prefix: Prefix): ParsedDateTime {
  const label = prefix === "start" ? "Start" : "End";
  const day = values[`${prefix}Day`].trim();
  const month = values[`${prefix}Month`].trim();
  const year = values[`${prefix}Year`].trim();
  const hour = values[`${prefix}Hour`];
  const minute = values[`${prefix}Minute`];
  const result: ParsedDateTime = {};

  const missing = [!day && "day", !month && "month", !year && "year"].filter(Boolean) as string[];
  if (missing.length === 3) {
    result.dateError = `Enter ${prefix === "start" ? "a start" : "an end"} date`;
  } else if (missing.length > 0) {
    result.dateError = `${label} date must include a ${missing.join(" and a ")}`;
  } else if (!/^\d{1,2}$/.test(day) || !/^\d{1,2}$/.test(month) || !/^\d{4}$/.test(year)) {
    result.dateError = `${label} date must be a real date, like 8 12 2026`;
  } else if (!isRealDate(Number(year), Number(month), Number(day))) {
    result.dateError = `${label} date must be a real date`;
  }

  if (!hour || !minute) {
    result.timeError = `Select ${prefix === "start" ? "a start" : "an end"} time`;
  }

  if (!result.dateError && !result.timeError) {
    result.date = ukLocalToDate(Number(year), Number(month), Number(day), Number(hour), Number(minute));
  }
  return result;
}

/** Date rules that need more than one field. Kept separate so they always run (see validateRequestForm). */
export function dateErrors(values: RequestFormValues, now: Date): Partial<Record<keyof RequestFormValues, string>> {
  const errors: Partial<Record<keyof RequestFormValues, string>> = {};
  const start = parseDateTime(values, "start");
  const end = parseDateTime(values, "end");

  if (start.dateError) errors.startDay = start.dateError;
  if (start.timeError) errors.startHour = start.timeError;
  if (end.dateError) errors.endDay = end.dateError;
  if (end.timeError) errors.endHour = end.timeError;

  const current = now.getTime();
  if (start.date) {
    if (start.date.getTime() <= current) errors.startDay = "Start date and time must be in the future";
    else if (start.date.getTime() > current + RULES.maximumDaysInAdvance * 86_400_000)
      errors.startDay = `Start date must be within the next ${RULES.maximumDaysInAdvance} days`;
  }

  if (start.date && end.date) {
    const minutes = (end.date.getTime() - start.date.getTime()) / 60_000;
    if (minutes <= 0) errors.endDay = "End date and time must be after the start date and time";
    else if (minutes < RULES.minimumDurationMinutes)
      errors.endDay = `The booking must be at least ${RULES.minimumDurationMinutes} minutes long`;
    else if (minutes > RULES.maximumDurationDays * 24 * 60)
      errors.endDay = `The booking must be ${RULES.maximumDurationDays} days or shorter`;
  }
  return errors;
}

/**
 * Validates the whole form and returns the first message for each field.
 * Field rules (Zod) and date rules run independently, so a person who submits an empty form sees
 * every problem at once. (A Zod superRefine would be skipped while any other field is invalid.)
 */
export function validateRequestForm(values: RequestFormValues, now: Date = new Date()): Partial<Record<keyof RequestFormValues, string>> {
  const errors: Partial<Record<keyof RequestFormValues, string>> = {};
  const result = requestFieldSchema.safeParse(values);
  if (!result.success) {
    for (const issue of result.error.issues) {
      const field = issue.path[0] as keyof RequestFormValues;
      errors[field] ??= issue.message;
    }
  }
  for (const [field, message] of Object.entries(dateErrors(values, now))) {
    errors[field as keyof RequestFormValues] ??= message;
  }
  return errors;
}

/** react-hook-form resolver built on validateRequestForm. */
export function requestFormResolver(now: () => Date = () => new Date()): Resolver<RequestFormValues> {
  return async (values) => {
    const errors = validateRequestForm(values, now());
    const entries = Object.entries(errors);
    if (entries.length === 0) return { values, errors: {} };
    return {
      values: {},
      errors: Object.fromEntries(entries.map(([field, message]) => [field, { type: "validation", message }])),
    };
  };
}

export const emptyRequestForm: RequestFormValues = {
  costCode: "",
  budgetHolderName: "",
  budgetHolderEmail: "",
  regionId: "",
  siteId: "",
  buildingId: "",
  startDay: "",
  startMonth: "",
  startYear: "",
  startHour: "",
  startMinute: "",
  endDay: "",
  endMonth: "",
  endYear: "",
  endHour: "",
  endMinute: "",
  sinkRequired: undefined as unknown as "yes",
  clinicalCouchRequired: undefined as unknown as "yes",
  additionalRequirements: "",
  acceptTerms: false as unknown as true,
};

/** Order of fields on the page, so the error summary lists problems top to bottom. */
export const FIELD_ORDER: (keyof RequestFormValues)[] = [
  "costCode",
  "budgetHolderName",
  "budgetHolderEmail",
  "regionId",
  "siteId",
  "buildingId",
  "startDay",
  "startHour",
  "endDay",
  "endHour",
  "sinkRequired",
  "clinicalCouchRequired",
  "additionalRequirements",
  "acceptTerms",
];

/** Maps API field names (from a 400 response) to the form field that shows the error. */
export const API_FIELD_TO_FORM_FIELD: Record<string, keyof RequestFormValues> = {
  costCode: "costCode",
  budgetHolderName: "budgetHolderName",
  budgetHolderEmail: "budgetHolderEmail",
  buildingId: "buildingId",
  startsAt: "startDay",
  endsAt: "endDay",
  sinkRequired: "sinkRequired",
  clinicalCouchRequired: "clinicalCouchRequired",
  additionalRequirements: "additionalRequirements",
  acceptTerms: "acceptTerms",
};

export function toPayload(values: RequestFormValues): CreateRequestPayload {
  const start = parseDateTime(values, "start").date;
  const end = parseDateTime(values, "end").date;
  if (!start || !end) throw new Error("Dates must be validated before building the payload");
  return {
    costCode: values.costCode.trim().toUpperCase(),
    budgetHolderName: values.budgetHolderName.trim(),
    budgetHolderEmail: values.budgetHolderEmail.trim(),
    buildingId: Number(values.buildingId),
    startsAt: start.toISOString(),
    endsAt: end.toISOString(),
    sinkRequired: values.sinkRequired === "yes",
    clinicalCouchRequired: values.clinicalCouchRequired === "yes",
    additionalRequirements: values.additionalRequirements.trim() || null,
    acceptTerms: values.acceptTerms,
  };
}
