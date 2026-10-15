# Lab requirements (checklists)

Condensed from the course methodology. **Lab numbers inside the PDFs are outdated** — use this mapping:
lab 2 = data access (ADO.NET + EF Core), lab 3 = Web API, lab 4 = Angular, lab 5 = desktop (WPF/Avalonia).
Labs 2 and 3 are submitted and defended together, so lab 2 scenarios are demonstrated through the API.

Reports (Ukrainian, written by the author) need: commented key code fragments, results/screenshots,
negative scenarios, conclusions. Keep implementations explainable.

## Lab 2 — data access module (ADO.NET + EF Core)

Schema
- [ ] ≥ 5 tables, ≥ 4 foreign keys, ≥ one 1:N, ≥ one M:N through an **explicit** join entity with a **composite key** (Enrollment, CourseSign)
- [ ] ≥ one unique constraint (unique index on a business identifier), ≥ one concurrency token (`rowversion` on Cohort)
- [ ] types, required/optional, max lengths, decimal precision, FKs, deliberate DeleteBehavior, indexes
- [ ] EF Core Code First, initial migration applied
- [ ] seed: ≥ 10 rows in reference/main tables via `HasData`; ≥ 30 rows in dependent tables via `UseSeeding`/`UseAsyncSeeding`; data must allow filtering, grouping, pagination

ADO.NET (Microsoft.Data.SqlClient)
- [ ] CRUD for two related entities (CourseDay, DayExercise): async, `CancellationToken`, parameterized, `SqlDataReader`
- [ ] JOIN of ≥ 3 tables with filters and sorting
- [ ] aggregate query with GROUP BY and ≥ 2 aggregate functions
- [ ] transaction over ≥ 3 tables with separate **Commit** and **Rollback** scenarios (create course with program)

EF Core
- [ ] `DbContext` + separate `IEntityTypeConfiguration<T>` for main entities
- [ ] CRUD for ≥ 3 entities
- [ ] one complex business operation on an entity graph inside a transaction
- [ ] ≥ 6 LINQ queries: multi-parameter filter, sorting, DTO projection, related data, GroupBy with aggregates, pagination; ≥ 2 read-only queries with `AsNoTracking`
- [ ] optimistic concurrency: `DbUpdateConcurrencyException` on two separate `DbContext`s, with a clear reaction (409)
- [ ] second migration (new property / index / table) + generated SQL script
- [ ] reverse engineering: `dotnet ef dbcontext scaffold ... --no-onconfiguring` into a separate folder/project
- [ ] SQL for ≥ 3 LINQ queries via `ToQueryString` or logging, explained; no N+1 in demo scenarios

## Lab 3 — Web API

- [ ] ASP.NET Core Web API with external OAuth 2.0 / OIDC (Google, Entra ID, GitHub, Keycloak or Auth0)
- [ ] policy-based **and** resource-based authorization via a custom `IAuthorizationHandler` (author or Oracle may edit/delete)
- [ ] Postman collection, all endpoints demonstrated (JSON); fragment in the report
- [ ] OpenAPI docs (`Microsoft.AspNetCore.OpenApi` + Scalar), requests runnable from the docs UI
- [ ] `[ProducesResponseType<T>]`, `[Produces]`, `[Consumes]`, XML comments; every action declares all status codes
- [ ] DI: business logic in services behind interfaces; justify each lifetime in the report
- [ ] domain models vs DTOs (separate read / create / update DTOs) + mapping (Mapperly, Mapster or manual); explain why DTO ≠ model
- [ ] validation (DataAnnotations and/or FluentValidation), show the response to bad input
- [ ] global exception handling via `IExceptionHandler`; no exception details in Production
- [ ] pagination, filtering, sorting on collection endpoints with page metadata
- [ ] HTTP semantics: 201 + Location, 204 for delete, 404, 409 for uniqueness conflicts, idempotent PUT/DELETE
- [ ] load tests (k6 / NBomber / Bombardier) measured **before and after** an optimization (e.g. horoscope caching)

## Lab 4 — Angular 22 (web app for Wanderers)

- [ ] standalone workspace with routing and SCSS; angular-eslint (flat config)
- [ ] `core/` `shared/` `features/` structure; components, service, pipe, directive, guard, resolver, interceptor generated via CLI
- [ ] routing with a route parameter and lazy loading (`loadComponent` / `loadChildren`)
- [ ] functional guard and resolver (or a justified loading strategy)
- [ ] `HttpClient` + functional interceptor for a cross-cutting concern (auth token)
- [ ] REST CRUD for the main entity: list, details, create, update, delete
- [ ] Reactive Form with ≥ 3 validators + a server-side validation error scenario
- [ ] signals/computed or Observable + AsyncPipe; explain state ownership
- [ ] loading, empty, error and success states
- [ ] one custom pipe, one directive, one reusable shared component
- [ ] environment config for the API URL; no secrets in the bundle
- [ ] keyboard navigation and resize-friendly main screens
- [ ] ≥ 3 tests: service/HTTP, component/form, guard/pipe/directive; `ng test` and production `ng build` pass

Grading: architecture 1.0, routing/lazy 1.5, REST CRUD 2.0, forms 1.5, state 1.0, interceptor/config/security 1.0, pipe/directive/shared 0.75, tests+build 0.75, report 0.5.

## Lab 5 — desktop app (WPF, or Avalonia if approved) for Starshepherds

May cover only a sub-feature of the variant, but every item below is required.
- [ ] .NET 10 desktop app; folders Models, ViewModels, Views, Services, Mapping, Converters, Controls, Resources
- [ ] Grid-based layout that reacts to resize (no fake positioning with large margins)
- [ ] shared brushes/styles in resources; Style and DataTemplate
- [ ] ViewModel with PropertyChanged and ObservableCollection
- [ ] list (DataGrid/ListView) bound via ItemsSource and SelectedItem
- [ ] Add, Save/Edit, Delete, Refresh via ICommand; ≥ one command with CanExecute
- [ ] form with TwoWay binding and ≥ 3 validation rules
- [ ] reusable UserControl with a dependency property (Avalonia: StyledProperty) or command binding
- [ ] IValueConverter (or a justified trigger/formatting alternative)
- [ ] AutoMapper Profile mapping between two layers
- [ ] service interface injected into the ViewModel via constructor
- [ ] one async I/O operation with IsBusy and an error state
- [ ] ≥ 3 ViewModel unit tests: happy path, validation/CanExecute, service failure

Grading includes MVVM architecture 2.0 (separation of concerns, testability).
