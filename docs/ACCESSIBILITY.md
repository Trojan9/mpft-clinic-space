# Accessibility

Target: **WCAG 2.2 level AA**, the standard the Public Sector Bodies Accessibility Regulations 2018 require.

## Checklist

| Area | What's done | WCAG |
|---|---|---|
| Structure | Landmarks (`header`, `nav` with labels, `main`, `footer`); one `h1` per page; logical heading order | 1.3.1, 2.4.6 |
| Navigation | "Skip to main content" link; focus moves to the `h1` on every route change, so screen readers announce the new page | 2.4.1, 2.4.3 |
| Page titles | Unique titles, prefixed "Error:" when the form has errors | 2.4.2, 3.3.1 |
| Forms | Every field has a visible `<label>`; hints and errors linked with `aria-describedby`; groups use `fieldset`/`legend`; `aria-invalid` on fields with errors; correct `autocomplete` on sign-in | 1.3.1, 3.3.2, 1.3.5 |
| Errors | Error summary takes focus, lists problems in page order, and links to each field; messages explain how to fix the problem | 3.3.1, 3.3.3 |
| Dates | Separate day/month/year inputs with `inputmode="numeric"`; no native date picker | 1.3.1, 2.1.1 |
| Keyboard | Everything works with the keyboard; visible high-contrast yellow focus state; no keyboard traps | 2.1.1, 2.4.7, 2.4.11 |
| Colour | Text contrast at least 4.5:1 (NHS palette); status tags show words, not just colour | 1.4.3, 1.4.1 |
| Targets | Radio buttons and checkboxes are 40×40 px; buttons at least 44 px tall | 2.5.8 |
| Responsive | Works at 320 px wide and at 400% zoom with no horizontal scrolling; tables become cards | 1.4.10 |
| Motion | `prefers-reduced-motion` turns off animation | 2.3.3 |
| Live updates | Loading, results counts and success messages use `role="status"`; problems use `role="alert"`; the character count is a polite live region | 4.1.3 |
| Language | `lang="en-GB"` | 3.1.1 |

## Automated checks

- **ESLint `jsx-a11y`** at build time.
- **vitest-axe** in component tests (sign in, request form, My Requests, administrator review).
- **@axe-core/playwright** in a real browser against the running app, with the WCAG 2.0/2.1/2.2 A and AA rule sets, including colour contrast.
- A Playwright keyboard test for the skip link.

Automated tools find around a third of accessibility issues. Before going live I'd want manual testing with NVDA + Chrome, JAWS, VoiceOver on iOS, 400% zoom, Windows high-contrast mode, and an accessibility statement.
