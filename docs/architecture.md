# Architecture

Clean Architecture, four layers, dependencies point inward only. See `CLAUDE.md` → "Architecture
rules" for the rules; this page is the map and one worked example.

## Layers

```
┌──────────────────────────────────────────────────────────────┐
│ Api             controllers, auth (JWT/policies/claims),      │
│                 OpenAPI, the one global exception handler.    │
│                 The composition root.                         │
├──────────────────────────────────────────────────────────────┤
│ Infrastructure  EF Core DbContext + entity configs,            │
│                 migrations, seeding, ADO.NET repositories,     │
│                 external HTTP clients.                         │
├──────────────────────────────────────────────────────────────┤
│ Application     use cases (MediatR commands/queries),          │
│                 validators, DTOs, interfaces                   │
│                 (IAppDbContext, ICurrentUser, ...).            │
├──────────────────────────────────────────────────────────────┤
│ Domain          entities, enums, business-rule methods.        │
│                 References nothing.                            │
└──────────────────────────────────────────────────────────────┘
      Api → Infrastructure → Application → Domain
```

Each arrow is a project reference. `Api` is the only project allowed to see `Infrastructure`; it
wires `Infrastructure`'s implementations to the interfaces `Application` declares.

## Where things live

| Kind of code | Location |
|---|---|
| Entities, enums, domain methods | `Domain/Entities/`, `Domain/Enums/` |
| One use case (command/query + validator + handler, one file) | `Application/<Feature>/` |
| Cross-cutting Application types (`IAppDbContext`, `ICurrentUser`, `NotFoundException`, `PagedResult<T>`, `ValidationBehavior`) | `Application/Common/` |
| EF Core entity mapping | `Infrastructure/Persistence/Configurations/` |
| `DbContext`, migrations | `Infrastructure/Persistence/`, `Infrastructure/Persistence/Migrations/` |
| Seeding | `Infrastructure/Persistence/Seeding/` (test data via `UseSeeding`/`UseAsyncSeeding`); `HasData` inside the relevant `Configurations/*.cs` (reference data) |
| ADO.NET repositories | `Infrastructure/Ado/` |
| Controllers | `Api/Controllers/` |
| Auth (JWT setup, claims transformation, policies, resource-based handler) | `Api/Auth/` |
| Global exception handling | `Api/ExceptionHandling/` |
| Tests | `api/tests/AstraKa.Tests/Common/` (shared fixture), `.../Ado/`, `.../Auth/`, `.../Endpoints/`, `.../Concurrency/` |

## Walkthrough: `PUT /api/v1/exercises/{id}`

1. **`ExercisesController.Update`** (`Api/Controllers/ExercisesController.cs`) receives the request.
   `[Authorize]` has already passed - the JWT was validated, and `DatabaseClaimsTransformation`
   (`Api/Auth/`) added the caller's internal `UserId`/`Role` claims if they're onboarded.
2. The controller sends `GetExerciseByIdQuery(id)` through `ISender`. MediatR's pipeline runs
   `ValidationBehavior<,>` (`Application/Common/ValidationBehavior.cs`) first - no validator is
   registered for this query, so it passes straight through - then
   `GetExerciseByIdQueryHandler` (`Application/Exercises/GetExerciseById.cs`) queries
   `IAppDbContext.Exercises`. If nothing matches, it throws `NotFoundException`, which ends the
   request right here (step 7) with a 404 - the route-missing case ends before any authorization
   check runs.
3. Otherwise the controller has an `ExerciseDto` with the exercise's `AuthorId`. It calls
   `IAuthorizationService.AuthorizeAsync(User, exercise.AuthorId, Policies.AuthorOrOracle)`
   (`Api/Auth/Policies.cs`). ASP.NET Core invokes `AuthorOrOracleAuthorizationHandler`
   (`Api/Auth/AuthorOrOracleAuthorizationHandler.cs`), which succeeds if the caller's `Role` claim
   is `Oracle`, or their `UserId` claim equals the resource's author id.
4. If that fails, the controller returns `Forbid()` - 403, no further MediatR call.
5. If it succeeds, the controller builds `UpdateExerciseCommand(id, request.Name, ...)` from the
   route `id` plus the body-only `UpdateExerciseRequest` (same file) and sends it.
6. `ValidationBehavior<,>` runs again, this time against `UpdateExerciseCommandValidator` (also in
   `Application/Exercises/UpdateExercise.cs`). A failing rule throws FluentValidation's
   `ValidationException`, ending at step 7 with a 400 and field errors.
7. `UpdateExerciseCommandHandler` loads the `Exercise` entity via `IAppDbContext.Exercises.FindAsync`,
   calls `exercise.Update(...)` (`Domain/Entities/Exercise.cs` - the one method that enforces the
   non-empty-name invariant), then `SaveChangesAsync`. EF Core persists the change through
   `AppDbContext`/`ExerciseConfiguration` (`Infrastructure/Persistence/`).
8. Any exception raised anywhere above - `NotFoundException`, `ValidationException`,
   `DbUpdateConcurrencyException`, a unique/FK violation reaching SQL Server directly, or anything
   unexpected - is caught by `GlobalExceptionHandler`
   (`Api/ExceptionHandling/GlobalExceptionHandler.cs`), the one place that turns it into the right
   ProblemDetails status (404/400/409/500). On success, the controller returns `204 NoContent`.
