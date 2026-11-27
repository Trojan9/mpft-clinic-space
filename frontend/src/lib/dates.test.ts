import { describe, expect, it } from "vitest";
import { formatDate, formatPeriod, formatShortPeriod, formatTime, isRealDate, ukLocalToDate } from "./dates";

describe("ukLocalToDate", () => {
  it("treats winter times as GMT", () => {
    expect(ukLocalToDate(2026, 12, 8, 9, 0).toISOString()).toBe("2026-12-08T09:00:00.000Z");
  });

  it("treats summer times as BST (UTC+1)", () => {
    expect(ukLocalToDate(2026, 7, 14, 9, 30).toISOString()).toBe("2026-07-14T08:30:00.000Z");
  });

  it("handles the day the clocks go forward", () => {
    // 29 March 2026: clocks go from 01:00 GMT to 02:00 BST.
    expect(ukLocalToDate(2026, 3, 29, 0, 30).toISOString()).toBe("2026-03-29T00:30:00.000Z");
    expect(ukLocalToDate(2026, 3, 29, 3, 0).toISOString()).toBe("2026-03-29T02:00:00.000Z");
  });

  it("handles the day the clocks go back", () => {
    // 25 October 2026: clocks go from 02:00 BST to 01:00 GMT.
    expect(ukLocalToDate(2026, 10, 25, 9, 0).toISOString()).toBe("2026-10-25T09:00:00.000Z");
  });
});

describe("isRealDate", () => {
  it.each([
    [2026, 2, 28, true],
    [2028, 2, 29, true],
    [2026, 2, 29, false],
    [2026, 4, 31, false],
    [2026, 13, 1, false],
    [2026, 0, 10, false],
    [2026, 1, 0, false],
  ])("%i-%i-%i is %s", (y, m, d, expected) => {
    expect(isRealDate(y, m, d)).toBe(expected);
  });
});

describe("formatting follows the NHS style guide", () => {
  it("formats dates in full", () => {
    expect(formatDate("2026-12-08T09:00:00Z")).toBe("Tuesday 8 December 2026");
  });

  it.each([
    ["2026-12-08T09:00:00Z", "9am"],
    ["2026-12-08T12:00:00Z", "12pm"],
    ["2026-12-08T00:00:00Z", "12am"],
    ["2026-12-08T14:30:00Z", "2:30pm"],
    ["2026-07-14T08:30:00Z", "9:30am"],
  ])("formats %s as %s", (iso, expected) => {
    expect(formatTime(iso)).toBe(expected);
  });

  it("names the day once for a same-day period", () => {
    expect(formatPeriod("2026-12-08T09:00:00Z", "2026-12-08T17:00:00Z")).toBe("Tuesday 8 December 2026, 9am to 5pm");
  });

  it("names both days for a multi-day period", () => {
    expect(formatShortPeriod("2026-12-08T09:00:00Z", "2026-12-09T17:00:00Z")).toBe("8 Dec 2026 9am to 9 Dec 2026 5pm");
  });
});
