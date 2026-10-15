# Domain model

Source of truth for the AstraKa data model. Conceptual diagram: `diagrams/conceptual-diagram.png`
(editable: `diagrams/conceptual-diagram.drawio`). Target: SQL Server, 3rd normal form.

Types and delete behaviors below are **proposals** for the physical model; adjust them
deliberately and keep this file in sync.

## Entities

### User
Every person who signs in. Authentication is done by an external OIDC provider.

| Attribute | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| ExternalId | nvarchar(200) | `sub` claim from the provider's JWT. **Unique.** |
| Email | nvarchar(256) | **Unique** |
| Name | nvarchar(100) | Display name / nickname |
| Role | enum → nvarchar(20) | `Wanderer`, `Starshepherd`, `Oracle` |

Oracle is just a User with role `Oracle`; it has no profile table and no UI (works via Scalar/Postman,
created by seeding).

### Wanderer (client profile, 1:1 with User)
| Attribute | Type | Notes |
|---|---|---|
| UserId | Guid | PK + FK → User |
| ZodiacSignId | int | FK → ZodiacSign ("Born under") |

Birth date is **not** stored — only the sign (12 options, not 365).

### Starshepherd (specialist profile, 1:1 with User)
| Attribute | Type | Notes |
|---|---|---|
| UserId | Guid | PK + FK → User |
| Title | nvarchar(100) | Self-invented title, free text ("Grand Keeper of the Seventh Orbit") |

### ZodiacSign (reference table, seeded with HasData)
| Attribute | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | nvarchar(20) | **Unique**: Aries … Pisces |
| SequenceNumber | int | 1–12, **unique**. Aries = 1 |

Derived, **not stored**: azimuth of the "astral address" = `(SequenceNumber - 1) * 30`
(compass degrees, 0° = north, clockwise; Libra = 180°, Scorpio = 210°). Element (fire/water/…) is
also derived from the sign if ever shown.

### Exercise (catalog)
| Attribute | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| AuthorId | Guid? | FK → Starshepherd. **Null = system exercise** (seeded, editable only by Oracle) |
| Name | nvarchar(100) | |
| Description | nvarchar(2000) | |
| Unit | enum → nvarchar(20) | What is counted: `Reps`, `Seconds`, `Breaths` |
| VideoUrl | nvarchar(500)? | Demo video by the author; the file lives in blob/file storage, DB keeps only the link |

### Course (program template)
| Attribute | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| AuthorId | Guid | FK → Starshepherd ("Leads") |
| Name | nvarchar(150) | |
| Description | nvarchar(2000) | |
| Promise | nvarchar(500) | What this course promises |
| Price | decimal(10,2) | Set by the author |
| Status | enum → nvarchar(20) | `Draft`, `Published` (enrollment open) |

### CourseSign (explicit M:N Course ↔ ZodiacSign, "Open to")
| Attribute | Type | Notes |
|---|---|---|
| CourseId | Guid | **Composite PK**, FK → Course |
| ZodiacSignId | int | **Composite PK**, FK → ZodiacSign |

No rows for a course = **Any sign**. "Course for water signs" = rows for Cancer, Scorpio, Pisces.

### Cohort (a run of a course; UI name **Constellation**)
| Attribute | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| CourseId | Guid | FK → Course ("Runs as") |
| StartDate | date | |
| SeatLimit | int | > 0 |
| SeatsTaken | int | Counter incremented on enrollment |
| RowVersion | rowversion | **Concurrency token** |

`SeatsTaken` is a deliberate, documented denormalization: it exists so that two simultaneous
enrollments for the last seat both update the **same row**, and the rowversion catches the
conflict (→ 409). Without it, rowversion on the cohort would never fire, because enrolling
only inserts into Enrollment.

Derived, not stored: end date = `StartDate + (number of course days) - 1`; phase
(not started / running / finished) from dates.

### CourseDay
| Attribute | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| CourseId | Guid | FK → Course ("Consists of") |
| DayNumber | int | 1-based. **Unique (CourseId, DayNumber)** |
| Title | nvarchar(150) | e.g. "Soul's departure from Earth orbit" |
| Description | nvarchar(2000)? | |
| MeetLink | nvarchar(500)? | Video meeting with the Starshepherd. A day may have only a meeting and no exercises |

The calendar date of a day is derived: `Cohort.StartDate + DayNumber - 1`.

### DayExercise
| Attribute | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| CourseDayId | Guid | FK → CourseDay ("Includes") |
| ExerciseId | Guid | FK → Exercise ("Used in") |
| RevelationId | Guid? | FK → Revelation ("Adds"). **Null = base program** (same for everyone) |
| OrderNumber | int | Order within the day |
| Sets | int | Number of sets, > 0 |
| Amount | int | Per set, in the exercise's `Unit` (reps / seconds / breaths), > 0 |

The same exercise may appear several times in one day, so there is **no** unique
(CourseDayId, ExerciseId). Base-program order can be unique with a filtered index:
(CourseDayId, OrderNumber) where RevelationId IS NULL.

### Enrollment (explicit M:N Wanderer ↔ Cohort)
| Attribute | Type | Notes |
|---|---|---|
| WandererId | Guid | **Composite PK**, FK → Wanderer ("Joins") |
| CohortId | Guid | **Composite PK**, FK → Cohort ("In") |
| EnrolledAt | datetime2 | |
| Status | enum → nvarchar(20) | Stored: `Active`, `Left` (UI: "Gave up") |
| PaidPrice | decimal(10,2) | Snapshot of `Course.Price` at enrollment — a separate fact, not a duplicate |

`Completed` is **computed**, not stored: status `Active` and the cohort has finished.

### Revelation
Personal note from the Starshepherd to one participant for a future day.

| Attribute | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| WandererId, CohortId | Guid, Guid | FK → Enrollment ("Receives") |
| CourseDayId | Guid | FK → CourseDay ("Targets") — the day it is meant for |
| Message | nvarchar(2000) | |
| CreatedAt | datetime2 | |

Extra exercises are ordinary DayExercise rows with `RevelationId` set.

### DailyReport
| Attribute | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| WandererId, CohortId | Guid, Guid | FK → Enrollment ("Submits") |
| Date | date | **Unique (WandererId, CohortId, Date)** — one per day per enrollment |
| HarmonyLevel | tinyint | 1–10. 1 = Chaos, middle = Imbalance, 10 = Harmony |

### ExerciseCompletion
| Attribute | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| DailyReportId | Guid | FK → DailyReport ("Contains") |
| DayExerciseId | Guid | FK → DayExercise ("Marks") |
| CompletedAt | datetime2 | |

Unique (DailyReportId, DayExerciseId).

## Delete behavior (proposals — watch SQL Server "multiple cascade paths")

- Course → CourseDay → DayExercise: **Cascade** (a program is owned by its course)
- Course → CourseSign: **Cascade**
- Exercise → DayExercise: **Restrict** (cannot delete an exercise that is used in a program)
- Course → Cohort, Cohort → Enrollment: **Restrict** (paid enrollments are never silently lost)
- Enrollment → DailyReport → ExerciseCompletion: **Cascade**
- Enrollment → Revelation, Revelation → DayExercise: choose one cascade path, make the other Restrict
- DayExercise → ExerciseCompletion: **Restrict** (or NoAction) to avoid a second cascade path
- User → Wanderer / Starshepherd: **Cascade**

## Business rules

- **Enroll**: course is `Published`; today < cohort `StartDate` (no joining after start);
  `SeatsTaken < SeatLimit`; the course is open to the Wanderer's sign (or has no sign rows);
  not already enrolled in this cohort (composite PK). Stores `PaidPrice`, increments `SeatsTaken`
  in the same transaction; a rowversion conflict → **409 Conflict** ("The last seat has been taken").
- Enrolling again into a **later cohort** of the same course is allowed.
- **Leave**: `Active → Left`. Money is never refunded.
- **Today's plan**: day number = today − `StartDate` + 1; shows base DayExercises of that day plus
  DayExercises added by Revelations for this enrollment, plus the day's MeetLink if any.
- **Daily report**: at most one per day per enrollment; completions may only reference that day's
  exercises (base + this enrollment's revelations).
- **Missed day** = a day that has exercises but no completions for the enrollment.
- **Revelation**: only the course author; only for a day that has not passed yet.
- **Editing**: courses and exercises can be edited/deleted by their author **or Oracle**;
  system exercises (no author) only by Oracle. This is the lab 3 resource-based authorization example.
- Every exercise is performed facing the Wanderer's astral address (azimuth from the sign).
  The UI only shows a hint ("Face 330° (NNW)"); nothing is verified.

## Use cases by role (API surface)

**Wanderer (web)**
- Browse published courses and upcoming cohorts (filter, sort, paginate)
- Enroll in a cohort; leave an enrollment
- My enrollments (active / completed / left)
- Today's plan for an enrollment; mark exercises; submit the daily report with harmony level
- Read Revelations addressed to me
- Daily horoscope for my sign
- Profile: name, zodiac sign

**Starshepherd (desktop)**
- CRUD own exercises (with demo video)
- CRUD own courses, sign restrictions, publish
- Edit the course program: days, day exercises *(ADO.NET)*; create a course with its full program in one transaction *(ADO.NET)*
- CRUD cohorts of own courses
- Participants of a cohort *(ADO.NET JOIN report)*; per-participant progress, missed days, harmony trend
- Course statistics *(ADO.NET GROUP BY report)*
- Write Revelations (optionally with extra exercises)
- Profile: name, title

**Oracle (API only)**
- Edit / delete any course or exercise, including system exercises

## Data access split

See `CLAUDE.md` → "ADO.NET vs EF Core boundary". Rule: one operation = one technology.

## Open / deferred

- Is a seat freed when a Wanderer leaves before the cohort starts? (default: no)
- Final name for the exercise "The Arch" and the rest of the exercise catalog (seed data)
- External auth provider (Auth0 vs Keycloak) — decide at lab 3
- Video storage implementation (local folder first, then Azure Blob / Azurite or MinIO)
- Optional CV technique check — not planned
