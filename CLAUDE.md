# AstraKa — project guide for Claude Code

## What this is

AstraKa is a university project (course «Конструювання програмного забезпечення», labs 2–5,
variant 19 "Home rehabilitation system") dressed up as a fictional astral sect,
**AstraKa — Temple of the Returning Star**. Under the costume it is an ordinary
home-rehabilitation system: a specialist builds exercise programs, clients follow them
day by day and report how they feel, the specialist watches progress and adjusts.

It is also meant for the author's portfolio, so code quality and a clean, runnable repo matter.

- Domain model, business rules, use cases: `docs/domain-model.md` (source of truth for the data model)
- Lore, naming, tone of voice, sample content: `docs/world-bible.md`
- What each lab must demonstrate: `docs/lab-requirements.md`
- Conceptual ER diagram: `docs/diagrams/conceptual-diagram.png`

## Language

- The **product is English-only**: UI text, API messages, seed data, identifiers, comments, commit messages.
- **Talk to the user in Ukrainian.** Lab reports are written in Ukrainian by the user.

## Repository layout

```
/api        .NET solution: AstraKa.Domain, .Application, .Infrastructure, .Api (+ tests/)
/web        Angular app for Wanderers (clients)
/desktop    Avalonia (or WPF) app for Starshepherds (specialists) (+ its own tests)
/docs       design docs, diagrams, lab requirements
```

Each app owns its tests: .NET test projects live under `/api/tests` and `/desktop/tests`,
Angular specs sit next to the code (`*.spec.ts`). The desktop app does **not** reference API
projects; it talks to the API over HTTP and has its own client-side models.

## Tech stack

- **.NET 10**, C# 14, **EF Core 10**, **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`, Windows)
- ADO.NET via **Microsoft.Data.SqlClient** (never System.Data.SqlClient)
- ASP.NET Core Web API, `Microsoft.AspNetCore.OpenApi` + **Scalar**
- **MediatR** (CQRS) + **FluentValidation**; API mapping with **Mapperly** or manual mapping
  (lab 3 lists Mapperly/Mapster/manual — not AutoMapper)
- Auth: external **OAuth 2.0 / OpenID Connect** provider (likely Auth0, not decided yet);
  the API only validates JWTs, no passwords are stored
- Horoscopes: freehoroscopeapi.com behind `IHoroscopeProvider` (fallback: API Ninjas)
- Web: **Angular 22**, standalone components, signals, **Spartan UI** (shadcn-style, Tailwind v4) + SCSS
- Desktop: **Avalonia** (pending lecturer approval; WPF fallback), MVVM with CommunityToolkit.Mvvm,
  **AutoMapper** (required by lab 5)
- MediatR and AutoMapper are under the Lucky Penny commercial license; the free Community
  edition covers this project. Without a license key they only log a warning.

## Architecture rules (API)

Clean Architecture with four projects. Dependencies point inward only:

- **Domain** — entities, enums, business rules (e.g. `cohort.EnsureHasFreeSeat()`). No references.
- **Application** — use cases (MediatR commands/queries), validators, DTOs, interfaces
  (`IAppDbContext`, `ICurrentUser`, `IHoroscopeProvider`, ADO.NET repository interfaces).
  References Domain only.
- **Infrastructure** — EF Core `DbContext`, `IEntityTypeConfiguration<T>` per entity, migrations,
  seeding, ADO.NET repositories, external HTTP clients. References Application.
- **Api** — thin controllers (`return await mediator.Send(...)`), auth, OpenAPI, exception handling.

### CQRS conventions — keep them boring

- **One use case = one file**: the command/query record, its validator and its handler live
  together, e.g. `Application/Enrollments/EnrollInCohort.cs`. Group files by feature, not by type
  (no `Commands/`, `Handlers/`, `Validators/` folders).
- Validation runs in **one** MediatR pipeline behavior; handlers never call validators manually.
- Handlers use `IAppDbContext` directly. **No generic repository, no Unit of Work wrapper,
  no specification pattern** — `DbContext` already is both.
- Errors: handlers throw only `NotFoundException` (404) or `DomainException` (409) — both single
  classes, no subtype hierarchy. Everything else (validation failures, `DbUpdateConcurrencyException`,
  a uniqueness/FK violation that reaches SQL Server directly) is matched by type in the one
  `IExceptionHandler` without a dedicated exception type. 403 is `ControllerBase.Forbid()`, not an
  exception. Never leak exception details in Production.
- Request body vs route values: bind the MediatR command straight from the body when nothing else
  needs merging in (e.g. `OnboardAsWandererCommand`, `CreateExerciseCommand`). When the route
  supplies a value the body must not duplicate (an `id`), use a body-only `XxxRequest` record with
  no `Id`, and have the controller build the full command from the route value + the request
  (e.g. `UpdateExerciseRequest` → `UpdateExerciseCommand`).
- Resource-based authorization stays in Api: the controller loads the resource first (a query that
  also gives the 404), calls `IAuthorizationService.AuthorizeAsync` with the resource's author id
  against a named policy from `Api/Auth/Policies.cs`, returns `Forbid()` on failure, only then sends
  the mutating command. `Application` never references `Microsoft.AspNetCore.Authorization` or any
  other ASP.NET Core type.
- Prefer clear, explainable code over clever code: the author has to defend every line at a
  lab defense. **Ask before adding a new library, layer or pattern.**
- Add short comments on the main structural pieces (lab reports require commented code).

### ADO.NET vs EF Core boundary

Lab 2 requires both. They share one database and one logical model.

- **ADO.NET** owns the *course program*:
  - CRUD of `CourseDay` and `DayExercise`
  - the transaction "create course with full program" (Course + CourseDays + DayExercises)
  - read-side reports: JOIN of 3+ tables (cohort participants) and GROUP BY statistics
- **EF Core** owns everything else (users, exercises, courses, cohorts, enrollments,
  revelations, daily reports, completions) and may *read* program tables.
- **One operation = one technology.** Never mix ADO.NET and EF Core inside one command or
  transaction. Different operations may write the same table with different technologies.
- ADO.NET code: always async, takes `CancellationToken`, fully parameterized, reads with
  `SqlDataReader`, handles nullable columns explicitly.

## Domain cheat sheet (details in docs/domain-model.md)

User (+ role) → Wanderer (client) / Starshepherd (specialist) / Oracle (admin, API-only, no UI).
Course = program template made of CourseDays → DayExercises. A course runs as Cohorts
(UI name: **Constellation**). Wanderers enroll into a Cohort (Enrollment, composite key).
Daily they mark completed exercises (ExerciseCompletion) and submit one DailyReport with a
Chaos–Imbalance–Harmony level (1–10). Starshepherds leave Revelations (personal notes for a
future day, optionally with extra DayExercises).

## Tone of voice (UI text, errors, empty states)

Deadpan: absurd content stated with a completely serious face, never winking, never overdone.
The sect is entirely fictional — no references to real religions or organizations.
Ordinary fields keep ordinary names (price, dates, email). Example:
"Your orbit has drifted. Return to practice before Saturn notices."

## Commands

Fill in as the projects are created.

```
# API
dotnet build api/AstraKa.slnx
dotnet test api/AstraKa.slnx
dotnet ef migrations add <Name> -p api/src/AstraKa.Infrastructure -s api/src/AstraKa.Api
dotnet ef database update -p api/src/AstraKa.Infrastructure -s api/src/AstraKa.Api

# Web
cd web && ng serve | ng test | ng build
```
