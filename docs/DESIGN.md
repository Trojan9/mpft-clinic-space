# UI design: research and decisions

## Starting point

The users are Trust staff: clinicians, administrators and managers. They use the tool now and then, often on shared, locked-down Windows desktops, and sometimes on a phone between appointments. They need to finish a short task first time and be confident it worked. That favours a plain, familiar, accessible interface over a bespoke visual identity.

## Research

I based the interface on the **NHS digital service manual**, the design system NHS services are built and tested with:

| Source | What I took from it |
|---|---|
| [Colour](https://service-manual.nhs.uk/design-system/styles/colour) | NHS Blue `#005EB8` header, text `#212B32`, secondary text `#4C6272`, green `#007F3B` primary buttons, red `#D5281B` errors, yellow `#FFEB3B` focus state. |
| [Typography](https://service-manual.nhs.uk/design-system/styles/typography) | Arial fallback stack (Frutiger is licensed), 16px body on mobile and 19px on desktop, bold headings with a clear scale. |
| [Error summary](https://service-manual.nhs.uk/design-system/components/error-summary) and [error message](https://service-manual.nhs.uk/design-system/components/error-message) | Summary at the top that takes focus and links to each field; a red bar and message beside the field; "Error:" in the page title; messages that say how to fix the problem ("Enter a cost code", not "Invalid"). |
| [Check answers pattern](https://service-manual.nhs.uk/design-system/patterns/check-answers) | Summary of everything entered, with *Change* links, before submission. |
| [Confirmation page](https://service-manual.nhs.uk/design-system/patterns/confirmation-page) | Green panel with the reference number, then "What happens next". |
| [Date input](https://service-manual.nhs.uk/design-system/components/date-input) | Separate day, month and year boxes rather than a native date picker, which many assistive technologies handle badly. |
| [Summary list](https://service-manual.nhs.uk/design-system/components/summary-list), [Tag](https://service-manual.nhs.uk/design-system/components/tag), [Inset text](https://service-manual.nhs.uk/design-system/components/inset-text), [Warning callout](https://service-manual.nhs.uk/design-system/components/warning-callout), [Details](https://service-manual.nhs.uk/design-system/components/details), [Character count](https://service-manual.nhs.uk/design-system/components/character-count), [Radios](https://service-manual.nhs.uk/design-system/components/radios), [Back link](https://service-manual.nhs.uk/design-system/components/back-link), [Pagination](https://service-manual.nhs.uk/design-system/components/pagination) | Built as React components with Tailwind, following each component's markup and accessibility guidance. |
| [Content style guide](https://service-manual.nhs.uk/content) | Plain English, sentence case, "9am" and "8 December 2026" formats, active voice. |
| GOV.UK *Question pages* and *Task list* research | Group related questions under clear headings when the form is short; split into more pages only if usability testing shows a need. |

Admin queue references: Jira / ServiceNow-style work queues, which open on "To review" with counts per status. The work that needs action is the default view.

## Decisions

1. **NHS look, Tailwind implementation.** The team uses Tailwind, so the NHS tokens (colours, type, focus) are defined as a Tailwind theme (`frontend/src/index.css`) and the components are written in React. This avoids pulling in the `nhsuk-frontend` CSS bundle and its jQuery-era JavaScript, and keeps one styling approach.
2. **One form page, grouped in cards**, then *Check your answers*. The form has 12 questions in 5 natural groups. Splitting it into 6 or more pages would slow down frequent requesters. The check answers step catches mistakes.
3. **Times in 15-minute steps from two drop-downs.** Rooms are booked in slots. Drop-downs avoid the format errors of free-text time entry and the inconsistent native `type=time` controls.
4. **The admin list opens on "To review", oldest first**: a first-in, first-out queue that's fair to requesters.
5. **Clash warning, not a block.** The data stops at building level, so overlapping requests might use different rooms. The administrator decides, with the information in front of them.
6. **Responsive tables** become labelled cards on small screens, so there's no horizontal scrolling.
7. **No logo artwork.** The header uses the service name. The NHS identity guidelines control use of the NHS logo, and that's a decision for the Trust's communications team.

## Screens

See `docs/screenshots/` and the README gallery.
