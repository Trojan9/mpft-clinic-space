/**
 * Date and time helpers. Bookings are for UK sites, so dates are entered and shown in UK time
 * (Europe/London) whatever time zone the browser is set to, and British Summer Time is handled.
 * Wording follows the NHS content style guide: "Tuesday 8 December 2026", "9am", "2:30pm".
 */

export const UK_TIME_ZONE = "Europe/London";

interface Parts {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
  weekday: string;
  monthName: string;
  monthShort: string;
}

const partsFormatter = new Intl.DateTimeFormat("en-GB", {
  timeZone: UK_TIME_ZONE,
  year: "numeric",
  month: "numeric",
  day: "numeric",
  hour: "numeric",
  minute: "numeric",
  hourCycle: "h23",
  weekday: "long",
});

const monthNames = [
  "January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December",
];

/** Breaks an instant into its UK wall-clock parts. */
export function ukParts(value: Date | string): Parts {
  const date = typeof value === "string" ? new Date(value) : value;
  const map: Record<string, string> = {};
  for (const part of partsFormatter.formatToParts(date)) map[part.type] = part.value;
  const month = Number(map.month);
  return {
    year: Number(map.year),
    month,
    day: Number(map.day),
    hour: Number(map.hour),
    minute: Number(map.minute),
    weekday: map.weekday ?? "",
    monthName: monthNames[month - 1] ?? "",
    monthShort: (monthNames[month - 1] ?? "").slice(0, 3),
  };
}

/** Minutes that UK time is ahead of UTC at the given instant (0 in winter, 60 in summer). */
function ukOffsetMinutes(utcMs: number): number {
  const p = ukParts(new Date(utcMs));
  const asUtc = Date.UTC(p.year, p.month - 1, p.day, p.hour, p.minute);
  return Math.round((asUtc - Math.floor(utcMs / 60000) * 60000) / 60000);
}

/** True if the numbers form a real calendar date (rejects 31 February, month 13 and so on). */
export function isRealDate(year: number, month: number, day: number): boolean {
  if (![year, month, day].every(Number.isInteger)) return false;
  if (year < 1900 || year > 2200 || month < 1 || month > 12 || day < 1) return false;
  const d = new Date(Date.UTC(year, month - 1, day));
  return d.getUTCFullYear() === year && d.getUTCMonth() === month - 1 && d.getUTCDate() === day;
}

/**
 * Converts a UK wall-clock date and time to the matching instant.
 * Runs the offset look-up twice so times next to a clock change resolve correctly.
 */
export function ukLocalToDate(year: number, month: number, day: number, hour: number, minute: number): Date {
  const wallClockAsUtc = Date.UTC(year, month - 1, day, hour, minute);
  let utc = wallClockAsUtc - ukOffsetMinutes(wallClockAsUtc) * 60000;
  utc = wallClockAsUtc - ukOffsetMinutes(utc) * 60000;
  return new Date(utc);
}

/** "Tuesday 8 December 2026" */
export function formatDate(value: Date | string): string {
  const p = ukParts(value);
  return `${p.weekday} ${p.day} ${p.monthName} ${p.year}`;
}

/** "8 Dec 2026", for tables. */
export function formatShortDate(value: Date | string): string {
  const p = ukParts(value);
  return `${p.day} ${p.monthShort} ${p.year}`;
}

/** "9am", "12pm", "2:30pm" */
export function formatTime(value: Date | string): string {
  const p = ukParts(value);
  const hour12 = p.hour % 12 === 0 ? 12 : p.hour % 12;
  const suffix = p.hour < 12 ? "am" : "pm";
  return p.minute === 0 ? `${hour12}${suffix}` : `${hour12}:${String(p.minute).padStart(2, "0")}${suffix}`;
}

/** "Tuesday 8 December 2026 at 9am" */
export function formatDateTime(value: Date | string): string {
  return `${formatDate(value)} at ${formatTime(value)}`;
}

/** Same day: "Tuesday 8 December 2026, 9am to 5pm". Different days: full start to full end. */
export function formatPeriod(start: Date | string, end: Date | string): string {
  const s = ukParts(start);
  const e = ukParts(end);
  const sameDay = s.year === e.year && s.month === e.month && s.day === e.day;
  return sameDay
    ? `${formatDate(start)}, ${formatTime(start)} to ${formatTime(end)}`
    : `${formatDateTime(start)} to ${formatDateTime(end)}`;
}

/** Compact period for tables: "8 Dec 2026, 9am to 5pm". */
export function formatShortPeriod(start: Date | string, end: Date | string): string {
  const s = ukParts(start);
  const e = ukParts(end);
  const sameDay = s.year === e.year && s.month === e.month && s.day === e.day;
  return sameDay
    ? `${formatShortDate(start)}, ${formatTime(start)} to ${formatTime(end)}`
    : `${formatShortDate(start)} ${formatTime(start)} to ${formatShortDate(end)} ${formatTime(end)}`;
}
