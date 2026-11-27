# ADR 0001: Technology stack

**Status:** Accepted

## Context
The brief asks for technology "broadly aligned" with the team. The job advert lists .NET/C#, SQL Server, React, React Native, TypeScript and HTML/CSS/Tailwind.

## Decision
- **API:** ASP.NET Core on **.NET 10 (LTS)**, with controllers.
- **Data:** **SQL Server 2022** with **EF Core 10** code-first migrations.
- **Web:** **React 19 + TypeScript (strict) + Vite + Tailwind CSS 4**, React Router, TanStack Query, React Hook Form + Zod.
- **Runtime:** Docker Compose for local development; containers suitable for Azure App Service or Container Apps.

## Consequences
- A team member can pick it up without learning a new stack.
- .NET 10 is supported until November 2028. .NET 11 (a short-term release) was not chosen for a line-of-business app.
- SQL Server has no arm64 image, so Apple Silicon developers rely on Rosetta (documented).
