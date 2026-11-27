# Exercise data

`exercise-data.xlsx` is the workbook supplied with the exercise ("Practical Exercise Data - Applications Developer B6"), committed unchanged. It has two sheets:

| Sheet | Columns | Imported as |
|---|---|---|
| **Buildings** | Region, Site, Building | The Region → Site → Building hierarchy for the request form: 11 regions, 22 sites, 44 buildings |
| **Users** | Name, Email Address, Role, Password | 14 accounts: 4 administrators ("Admin") and 10 requesters. The email address is the username. |

## How it's loaded

On start-up (Docker Compose sets `Seed__ImportLocations` and `Seed__ImportUsers` to `true`), the API:

1. Imports the Buildings sheet if the Regions table is empty.
2. Imports the Users sheet, adding any account that doesn't exist yet. **Passwords are hashed with PBKDF2 before they're stored. The plain text is never saved or logged.**
3. Creates 6 sample requests (`Seed__CreateDemoData`) for the first two requesters alphabetically (Alex Evans and Casey Hughes), decided by an administrator, so every screen has data.

To import into a running system without resetting it (existing rows are left alone):

```bash
docker compose run --rm api import-locations /app/data/exercise-data.xlsx
docker compose run --rm api import-users /app/data/exercise-data.xlsx
```

To start again from the file: `docker compose down -v && docker compose up --build`.

## Accepted formats

- Excel `.xlsx`: worksheets are found by name (`Buildings`/`Locations`/`Sites` and `Users`/`Accounts`/`Staff`). For locations, the first sheet is used if none of those names match.
- CSV (UTF-8, with or without BOM) with the same columns.
- Column names are matched ignoring case, spaces and punctuation, so `Email Address`, `email_address` and `Email` all work. Extra columns are ignored.
- Blank rows are skipped. Rows with missing values, an invalid email or an unknown role are **reported (with the row number) and skipped**, never guessed. Duplicates are merged.
