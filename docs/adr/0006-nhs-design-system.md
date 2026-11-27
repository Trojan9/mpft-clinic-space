# ADR 0006: Follow the NHS digital service manual, built with Tailwind

**Status:** Accepted

## Context
The users are NHS staff. The UI must be accessible (a legal requirement for public sector bodies) and quick to learn.

## Decision
Use NHS service manual styles, components and patterns, built as React components with a Tailwind theme of NHS design tokens, rather than importing `nhsuk-frontend`.

## Consequences
- The interface is familiar, and the patterns are already tested with NHS users.
- One styling approach (Tailwind) that fits the team's stack; no extra global CSS or legacy JavaScript.
- The components need to be kept in step with service manual updates by hand. That's a reasonable cost for about 15 small components.
