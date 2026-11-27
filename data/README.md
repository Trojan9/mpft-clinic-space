# Location data

`locations.csv` holds the Region → Site → Building hierarchy that the request form uses.

## Using the supplied exercise data

The exercise comes with a location data file ("Practical Exercise Data - Application Developer B6").
To use it:

1. Save it in this folder as `locations.csv` or `locations.xlsx`.
2. If it is `.xlsx`, set `Seed__LocationsFile=/app/data/locations.xlsx` for the `api` service in `docker-compose.yml`.
3. Reset the database so it is imported again: `docker compose down -v && docker compose up --build`.

Or import it into a running system without a reset (new items are added; nothing is removed):

```bash
docker compose run --rm api import-locations /app/data/locations.xlsx
```

## Accepted format

- CSV (UTF-8, with or without BOM) or Excel `.xlsx` (first worksheet).
- A header row with a region, site and building column. Matching ignores case, spaces and
  punctuation, so `Region`, `Region Name`, `site_name`, `Building Name` all work.
- Extra columns are ignored. Blank or incomplete rows are skipped. Duplicates are merged.

## The file in this repository

The current `locations.csv` is **sample data** so the app runs straight away. Site names are
based on public information about MPFT locations; building names are made up. Replace it with the
supplied file for a real demonstration.
