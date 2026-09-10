# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`BackendEjemplo` is a didactic ASP.NET Core 10 REST API template (Repository → Service → Controller) over EF Core 10 / PostgreSQL. It exists to demonstrate correct patterns for every kind of EF relationship (1:1, 1:N, N:N) and a unified error/pagination contract, all documented heavily enough that an LLM can extend it — or bootstrap an equivalent project from scratch — without reintroducing bugs that were already found and fixed here.

**Read the docs before writing code — don't rediscover what they already answer:**
- [`BackendEjemplo/README.md`](BackendEjemplo/README.md) — stack, architecture, bounded contexts, endpoint tables, how to run.
- [`BackendEjemplo/ARCHITECTURE.md`](BackendEjemplo/ARCHITECTURE.md) — **the operating manual**. Full code templates for every layer, a bootstrap-from-zero section, a checklist for adding a bounded context, an audit checklist, and a catalog of real bugs found in this project (read section 7 before assuming a pattern is safe). Two rules from its top callouts matter for *any* session in this repo:
  - **`BackendEjemplo.Tests/` is opt-in.** Never create a test project or add tests to it unless the user explicitly asks — this template is also used to bolt endpoints onto real company APIs that haven't adopted testing yet.
  - **Never assume the project should be named "BackendEjemplo."** That's this repo's own name. If bootstrapping a new project from section 0, ask the user for the real name first.
- [`BackendEjemplo/VALIDATION.md`](BackendEjemplo/VALIDATION.md) — which C# types need `?` for `[Required]` to actually validate (value types silently don't; see below).
- [`ANGULAR.md`](ANGULAR.md) — contract reference for consuming this API from an Angular 18+ frontend.

## Commands

Run from the solution root (contains `BackendEjemplo.slnx`):

```bash
dotnet tool restore                                    # restores dotnet-ef (local tool, .config/dotnet-tools.json)
dotnet build BackendEjemplo.slnx                        # build everything
dotnet run --project BackendEjemplo/BackendEjemplo.csproj   # run the API (migrates DB on startup)
dotnet test BackendEjemplo.Tests/BackendEjemplo.Tests.csproj                          # full test suite
dotnet test BackendEjemplo.Tests/BackendEjemplo.Tests.csproj --filter "FullyQualifiedName~ClientServiceTests"  # one test class
dotnet test BackendEjemplo.Tests/BackendEjemplo.Tests.csproj --filter "FullyQualifiedName~ClientServiceTests.AddAsync_PersistsAndReturnsSuccess"  # one test
```

Migrations (from solution root):

```bash
dotnet tool run dotnet-ef migrations add <Name> --project BackendEjemplo/BackendEjemplo.csproj --startup-project BackendEjemplo/BackendEjemplo.csproj --output-dir Shared/Persistence/Migrations
dotnet tool run dotnet-ef database update --project BackendEjemplo/BackendEjemplo.csproj --startup-project BackendEjemplo/BackendEjemplo.csproj
```

Always open the generated migration `.cs` and read it before trusting it — see ARCHITECTURE.md section 5.

Connection string lives in user-secrets, not `appsettings.json`:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "host=localhost;port=5432;username=postgres;password=<pw>;database=postgres" --project BackendEjemplo/BackendEjemplo.csproj
```

## Architecture

Four independent **bounded contexts**, each illustrating one relationship shape, all sharing the same layering and the same `Shared/` infrastructure:

- `BotAudit` — simple CRUD, no relationships.
- `OneToManyExample` — `Client` → `Order` (FK + `Restrict`).
- `ManyToManyExample` — `Student` ↔ `Course` via an explicit join entity `Enrollment` (not EF's native skinny many-to-many — the join needs its own data and its own repository/service/controller).
- `OneToOneExample` — `Employee` ↔ `EmployeeProfile` via a unique FK on the dependent side (not a shared primary key).

Every bounded context has the same folder shape (`Controllers/`, `Services/`, `Persistence/Repositories/`, `Resources/`, `Mapping/`, `Domain/{Models,Enums,Repositories,Services}`), and the request flow is always: `Controller` validates `ModelState` → maps `Resource → Entity` → calls `Service` → `Service` applies business rules via `Repository` → `Repository` queries `AppDbContext` (the single, shared `DbContext`) → `Service` returns `BaseResponse<TEntity>` → `Controller` maps back to `Resource` and an HTTP status. New bounded contexts follow this exact shape — copy an existing one that matches the relationship type, don't design a new pattern (ARCHITECTURE.md section 1-2 has the checklist and templates).

Load-bearing conventions that aren't obvious from any single file (each is explained in depth, with the historical bug that motivated it, in ARCHITECTURE.md sections 4/6/7/9):

- **Errors are one contract everywhere**: validation, business failures, and 500s all return `ProblemDetails` with a `traceId`. Controllers never hand-roll `NotFound(...)`/`BadRequest(...)` — they call `this.ToProblem(result, statusCode)` or `ValidationProblem(ModelState)`. `BaseResponse<T>.IsConflict` is what routes a business failure to 409 vs 404.
- **Sorting**: every `*PageRequest` has `SortBy`/`SortDescending`; each `Service` defines its own whitelist of sortable columns and applies it with `QueryableSortExtensions.ApplySort`, which always falls back to a deterministic default order (never leaves a query unordered).
- **Date-range filters use `DateOnly`, not `DateTime`**, and are interpreted in a configurable *business timezone* (`America/Lima`, one constant in `DateOnlyExtensions`) via `ToStartOfBusinessDayUtc()`/`ToEndOfBusinessDayUtc()` — not naively as a UTC day. Get this wrong and date filters silently misclassify records created near midnight.
- **`[Required]` on a non-nullable value type never fires** (`RequiredAttribute` only checks for `null`; a missing `int`/`DateOnly`/`bool`/etc. in JSON binds to `default(T)`, not `null`). Every `Save*Resource` field like this must be declared nullable (`int?`, `DateOnly?`, ...) and unwrapped with `.Value` in the `Mapping` — see VALIDATION.md.
- **EF Core query-translation traps in filter lambdas**: don't interpolate (`$"..."`) an entity property inside a `Where` lambda — it compiles to `string.Format`, which Npgsql can't translate (runtime failure, not a build error); use `+` concatenation instead. Don't use `EF.Functions.*` (e.g. `ILike`) in a filter that gets unit-tested by compiling and invoking it against in-memory POCOs (see below) — those methods throw outside of real SQL translation.
- **Optimistic concurrency is automatic**: every entity gets a `uint "Version"` shadow property mapped to Postgres's `xmin` system column via `.IsRowVersion()` in `AppDbContext.OnModelCreating` — no per-entity opt-in, and the generated migration for it contains no real DDL.
- Enums always serialize as their string name, never the underlying int — this requires registering `JsonStringEnumConverter` in *two* places in `Program.cs` (`Mvc.JsonOptions` and `Http.Json.JsonOptions`, since `AddOpenApi()`'s schema generator only reads the latter).

If `BackendEjemplo.Tests/` exists (it does in this repo) and you touch a `Service`, extend its tests: they mock the `Repository`/`IUnitOfWork` and, for filters, use `RepositoryMockExtensions.CaptureListPageFilter`/`CaptureListPageOrderBy` to compile the real filter/sort expression and run it against in-memory POCOs — this is what catches inverted-filter and silently-dropped-clause bugs, not a mock returning canned data. See ARCHITECTURE.md section 8 for the full pattern and why `xunit.v3` + `AwesomeAssertions` were chosen over `xunit`/`FluentAssertions`.
