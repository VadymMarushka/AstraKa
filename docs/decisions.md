# Architecture decision records

Short records of decisions already reflected in the code, not a wishlist. See `docs/architecture.md`
for the layer map and `CLAUDE.md` for the rules these decisions follow from.

## 1. `IAppDbContext` directly in MediatR handlers, no generic repository/UoW

**Context**: every use case (Application/`<Feature>`) needs to read and write entities.
**Decision**: handlers depend on `IAppDbContext` (DbSets + `SaveChangesAsync`) directly - no
`IRepository<T>`, no `IUnitOfWork` wrapper.
**Why**: `DbContext` already *is* both a repository (`DbSet<T>` query/add/remove) and a unit of
work (`SaveChangesAsync` batches everything added in one scope). A CQRS handler is a single-use-case
consumer, not a general-purpose data-access client, so it doesn't need another layer standing
between it and EF Core.
**Trade-off**: Application now depends on EF Core (`Microsoft.EntityFrameworkCore` types appear in
every handler). Accepted deliberately - the alternative (repository interfaces per entity) adds a
layer that would just forward to EF Core anyway, for a project this size.

## 2. Repositories only for ADO.NET (`Infrastructure/Ado/`)

**Context**: the course *program* (days/exercises) and a few read-side reports are ADO.NET-owned,
per the lab's "both EF Core and ADO.NET" requirement.
**Decision**: ADO.NET code is wrapped in repository interfaces (`ICourseDayRepository`,
`ICourseProgramRepository`, ...) declared in Application, implemented in Infrastructure.
`Microsoft.Data.SqlClient` is never referenced from Application.
**Why**: raw ADO.NET (connections, `SqlDataReader`, `SqlException.Number`) is infrastructure
detail that would otherwise leak into every handler that touches program data. The repository
hides the SQL and the error-number matching behind a plain async method signature.
**Trade-off**: this is the *only* place this codebase uses the repository pattern - inconsistent
with #1 on purpose. EF Core already is a repository; raw ADO.NET isn't, so it still needs one.

## 3. One operation = one technology (EF Core or ADO.NET, never both)

**Context**: EF Core and ADO.NET share one database and one logical model.
**Decision**: a single command/handler/transaction uses exactly one of the two. Different
operations may touch the same table with different technologies, but never the same operation.
**Why**: EF Core's change tracker and a raw ADO.NET connection don't know about each other's
pending writes or transactions. Mixing them inside one logical operation means one half could
commit (or roll back) without the other knowing, which breaks the atomicity the operation is
supposed to have.
**Trade-off**: some duplication (e.g. both sides can "read" `CourseDays`) is accepted so that reads
stay cheap and writes stay unambiguous about which technology owns the transaction.

## 4. MediatR, one use case per file, one `ValidationBehavior`

**Context**: every feature needs a command/query, a validator, and a handler.
**Decision**: MediatR for CQRS; the record, its `AbstractValidator`, and its `IRequestHandler` live
together in one file per use case (`Application/<Feature>/DoThing.cs`); validation runs in exactly
one pipeline behavior (`ValidationBehavior<,>`), never called manually by a handler.
**Why**: "one use case = one file" keeps a feature reviewable in one screen instead of spread
across `Commands/`/`Handlers/`/`Validators/` folders. One validation behavior means handlers never
have to remember to validate - MediatR's pipeline guarantees it runs before `Handle` does.
**Trade-off**: finding "every command" means grepping by base interface, not by folder - acceptable
given how much it simplifies finding everything about *one* feature.

## 5. Lookup tables for stored enums, Domain keeps plain C# enums

**Context**: `CourseStatus`, `EnrollmentStatus`, `UserRole`, `ExerciseUnit` are stored columns.
**Decision**: each has both a C# `enum` in `Domain/Enums/` and a matching lookup table
(`CourseStatusLookup`, `EnrollmentStatusLookup`, ...) in `Infrastructure/Persistence/Lookups/`,
seeded via `HasData` and FK-referenced from the owning table (e.g. `Enrollment.Status` -> `EnrollmentStatuses.Id`).
**Why**: a real FK gives referential integrity a `tinyint`/`byte` column alone can't - the database
itself rejects a status value that doesn't exist. 3NF: the status's own facts (if it ever needs
more than a name) live in one place, not repeated per row.
**Trade-off**: one extra table and one `HasConversion<byte>` mapping per stored enum, versus just
constraining the column with a `CHECK`. Chosen so the lab's ER diagram actually shows the
relationship, and so a future "list all statuses" admin feature has a table to query.

## 6. `Cohort.SeatsTaken` - deliberate denormalization, caught by a rowversion

**Context**: a cohort's free-seat count could be computed as `COUNT(*)` over `Enrollments`, but
checking and reserving a seat has to be race-safe.
**Decision**: `SeatsTaken` is stored on `Cohort` itself, incremented by `TakeSeat()`; `Cohort.RowVersion`
is an EF Core concurrency token mapped to a SQL Server `rowversion` column.
**Why**: storing the count means two concurrent "take the last seat" requests load and update the
*same row*, so a `rowversion` mismatch is guaranteed to catch the conflict (`DbUpdateConcurrencyException`)
instead of two `COUNT(*)` reads both seeing "one seat left" and both succeeding.
**Trade-off**: `SeatsTaken` and the real enrollment count could in principle drift if ever written
outside `TakeSeat()`. Accepted because every write path goes through that one method.

## 7. Auth0 for identity; roles and profiles live in our own database, keyed by `sub`

**Context**: the app needs real authentication without storing passwords.
**Decision**: Auth0 issues and signs JWTs; the API only validates them (`options.Authority`/
`options.Audience`). `User.ExternalId` stores the token's `sub` claim; role (`Wanderer`/
`Starshepherd`/`Oracle`) and profile data live in our own `Users`/`Wanderers`/`Starshepherds`
tables, looked up by `sub` once per request (`DatabaseClaimsTransformation`). The *role* is decided
by which onboarding endpoint the caller hits (`POST /me/wanderer` vs `/me/starshepherd`), not by a
claim the caller can set.
**Why**: Auth0 knows "who you are"; it has no opinion on "what you can do in AstraKa" business-wise.
Keeping role/profile local means a caller can never forge a role via their identity provider token.
**Trade-off**: an extra DB lookup per request (mitigated by adding the result as claims for the
rest of that request) and a window where a signed-in caller is authenticated but has no role yet -
treated as a first-class state (`ICurrentUser.UserId`/`Role` nullable), not an error.

## 8. Authorization lives in Api, not Application

**Context**: several mutations need "the resource's author, or an Oracle" (`AuthorOrOracle`).
**Decision**: the controller loads the resource (which also naturally gives the 404), calls
`IAuthorizationService.AuthorizeAsync(User, resource.AuthorId, Policies.AuthorOrOracle)`, and
returns `Forbid()` on failure - all in Api. Application never references
`Microsoft.AspNetCore.Authorization` or any other ASP.NET Core type.
**Why**: "can this caller mutate this resource" is a framework-level HTTP concern (`403` vs the
business rules a handler enforces); keeping it there means Application stays testable and reusable
without an HTTP context. `ICurrentUser` (Application's own abstraction) is reserved for
*visibility/data-shaping* decisions (e.g. hiding a draft course), a different concern.
**Trade-off**: controllers are no longer "just forward to MediatR" - a mutating action often makes
two `ISender.Send` calls (load, then act) instead of one. Accepted so Application stays
framework-agnostic.

## 9. Business rules depending on the clock or other entity state -> Domain -> 409; pure shape checks -> FluentValidation -> 400

**Context**: some request rules are "is this field well-formed" (e.g. `SeatLimit > 0`); others are
"is this allowed right now" (e.g. a cohort can't be rescheduled into the past, a seat limit can't
drop below seats already taken).
**Decision**: FluentValidation validators only check context-free shape of the request itself.
Anything that depends on "today" or on other stored state is a guard method on the Domain entity
(e.g. `Cohort.EnsureScheduledInFuture`, `Course.Publish`), raising `DomainException` -> 409.
**Why**: one consistent split keeps the two exception types meaningful - 400 means "this request
can never succeed as written"; 409 means "this request is well-formed but conflicts with the
system's current state." Mixing clock-dependent checks into validators would blur that line and
scatter one family of rules across two layers.
**Trade-off**: a rule that's arguably "just" a range check (e.g. `SeatLimit > 0`) is deliberately
checked in *both* places - FluentValidation for the clean 400, and the entity's own constructor/guard
as defense-in-depth for any code path that doesn't go through the validator (e.g. seeding, tests).

## 10. `TimeProvider` for "today"

**Context**: several rules (`EnsureScheduledInFuture`, `GetPhase`, enrollment's `EnrolledAt`) need
the current date/time.
**Decision**: handlers take `TimeProvider` via DI (registered as `TimeProvider.System` in
`Application/DependencyInjection.cs`) instead of calling `DateTime.UtcNow` directly.
**Why**: `TimeProvider` is a BCL abstraction (.NET 8+), not a new library - and it's the one seam
that lets a test pin "today" (`FixedTimeProvider` in the test project) without any production code
caring that it's being tested.
**Trade-off**: every handler that needs "today" takes one more constructor parameter. No test in
this codebase currently needs to actually move the clock (seeded data and test scenarios both use
offsets from the real time), so this capability is built but mostly unexercised today.

## 11. Integration tests run against a real LocalDB, not mocks or an in-memory provider

**Context**: `IAppDbContext` handlers rely on real SQL Server behavior - rowversion concurrency
tokens, FK `Restrict` violations (547), unique violations (2627/2601), check constraints.
**Decision**: the shared test fixture (`DatabaseFixture`) drops and remigrates a real
`AstraKa_Tests` LocalDB database once per test run and runs every endpoint test against it through
a real `WebApplicationFactory`, instead of EF's in-memory provider or a mocked `IAppDbContext`.
**Why**: the in-memory provider doesn't enforce FKs, check constraints, or rowversion conflicts the
same way SQL Server does - and several features (the last-seat race, FK-violation-as-409 safety
nets) exist specifically to be caught by real database behavior. A mock would have to reimplement
that behavior to be worth anything.
**Trade-off**: tests are slower (real SQL Server round-trips, one full drop+remigrate+reseed per
run) and need a LocalDB instance available. Accepted since this is also how the app will actually
run in production - EF's in-memory provider is explicitly not meant to prove provider-specific
behavior.

## 12. Enrollment: one explicit transaction, optimistic concurrency instead of pessimistic locking, no custom Unit of Work

**Context**: enrolling touches two rows (new `Enrollment`, `Cohort.SeatsTaken` update) and must
survive two Wanderers racing for the last seat.
**Decision**: `EnrollCommandHandler` wraps the whole operation - checks and writes - in one
explicit `db.Database.BeginTransactionAsync()`/`CommitAsync()`/`RollbackAsync()` block, under SQL
Server's default READ COMMITTED isolation. The actual last-seat race is resolved optimistically by
`Cohort.RowVersion` (see #6), not by locking the row for the duration of the checks.
`IAppDbContext` exposes `Database` (EF's own `DatabaseFacade`) to make this possible, rather than a
custom `IUnitOfWork`/transaction-wrapper type.
**Why**: the transaction makes "enroll" one atomic unit for the lab report to point at, even though
a single `SaveChangesAsync` would already be atomic on its own - this is explicit for visibility,
not because it adds safety the implicit transaction didn't have. The race itself is caught by
rowversion because READ COMMITTED doesn't block the second reader from seeing a seat as free; only
the concurrent *write* to the same `Cohort` row conflicts.
**Trade-off**: a pessimistic lock (e.g. `SELECT ... WITH (UPDLOCK)`) would make the losing request
wait instead of fail, and could offer a nicer retry-without-409 experience - not used here, since
the task calls for the loser to get a clear `409` ("the last seat has just been taken"), not a
delayed success.
