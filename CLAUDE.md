# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository overview

Dark Store is a q-commerce (15–30 min grocery delivery) backend for Kostanay, Kazakhstan, built as a **modular monolith on .NET 10** following Clean Architecture with light CQRS via MediatR. The repository contains the backend API only; the Angular PWA lives elsewhere.

The README.md and `docs/` directory are written primarily in Russian; **all source code, comments, XML docs, and TODOs must be in English** (enforced by `.editorconfig`, `.cspell.json`, and a CI gate in `deploy-azure1.yml`/`deploy-azure2.yml`).

## Common commands

```powershell
# Build / restore (uses .NET 10 SDK pinned via global.json — 10.0.200, latestPatch)
dotnet build DarkStore.slnx

# Run the API locally (migrations apply automatically at startup)
dotnet run --project src/DarkStore.API

# Run all tests
dotnet test

# Run one test project
dotnet test tests/DarkStore.UnitTests

# Run a single test (xUnit filter)
dotnet test --filter "FullyQualifiedName~OrderTests.Create_Should_Set_Status_To_Pending"

# Coverage (Cobertura via XPlat Code Coverage)
dotnet test --collect:"XPlat Code Coverage"

# Required style/format check before pushing — CI blocks merges that fail this
dotnet format --verify-no-changes

# Vulnerability scan — CI fails on high/critical
dotnet list package --vulnerable

# EF Core migrations (always specify Infrastructure as project, API as startup-project)
dotnet ef migrations add <Name> --project src/DarkStore.Infrastructure --startup-project src/DarkStore.API
dotnet ef database update    --project src/DarkStore.Infrastructure --startup-project src/DarkStore.API

# Local SQL Server + Redis for dev
docker-compose up -d

# Documentation portal (MkDocs Material)
.\docs-serve.ps1            # localhost:8000
.\docs-serve.ps1 -Build     # static build into ./site
```

The solution file is `DarkStore.slnx` (the new XML format), not `.sln`.

## Architecture

### Layer dependency rule

```
API ──► Application ──► Domain
   └──► Infrastructure ──► Application
                          ──► Domain
```

`Domain` has zero project references. `Application` depends only on `Domain`. `Infrastructure` depends on `Application` (for interfaces) and `Domain`. `API` wires everything via `AddApplication()` and `AddInfrastructure(...)` extension methods. NetArchTest runs in CI to enforce these boundaries.

### Two-database split (Kazakhstan Law №94-V compliance)

This is the most consequential constraint in the codebase. Two `DbContext`s are deliberately separate and **must not be merged**:

- **`AppDbContext`** (`AzureConnection`) — business data in Azure SQL (Sweden Central). Holds `Orders`, `OrderItems`, `Couriers`, `Deliveries`. Stores only GUIDs (`UserId`, `CourierId`) — **no PII fields** (no `FullName`, `Phone`, `Email`, `IIN`).
- **`PersonalDataDbContext`** (`KzLocalConnection`) — personal data on a server physically located in Kazakhstan. Holds `Users`, `Addresses`, `CourierProfiles`. Never put business entities (`Order`, `Product`, `Inventory`) here.

EF entity configurations are routed by namespace: `AppDbContext` applies configurations whose namespace does **not** contain `PersonalData`; `PersonalDataDbContext` applies the opposite. When adding a new entity, place its EF configuration in `Infrastructure/Configurations/` under a namespace that matches the side it belongs to.

Application layer code accesses the business context only via the `IAppDbContext` interface (in `Application/Common/Interfaces`), keeping handlers testable without SQL Server.

### MediatR pipeline (order matters)

Registered in `Application/DependencyInjection.cs`:

```
Request → LoggingBehavior → ValidationBehavior → AuditBehavior → Handler
```

`AuditBehavior` only logs requests whose type name ends with `Command` — queries are skipped to avoid noise. When introducing a new write operation, name it `*Command`; reads should be `*Query`.

### CQRS feature folders

Each feature lives in `Application/<BoundedContext>/Commands/<Action>/` or `.../Queries/<Action>/` with four files: `*Command.cs` / `*Query.cs`, `*Handler.cs`, `*Validator.cs` (commands only), and `*Response.cs`. Follow the existing `Orders/Commands/PlaceOrder` and `Orders/Queries/GetOrderById` shape when adding new features.

### Middleware pipeline (order matters)

Defined in `Program.cs`:

```
ExceptionHandler → HSTS → HttpsRedirection → ResponseCompression
→ CORS → RateLimiter → SecurityHeaders → CorrelationId
→ SerilogRequestLogging → OutputCache → Authentication → Authorization
→ MapControllers → SignalR Hubs
```

CORS must come before auth (preflight handling). `AllowCredentials()` is required for SignalR WebSocket negotiation. Response compression must precede CORS so it sees correct headers.

### Background jobs

Quartz.NET with a **persistent store on Azure SQL** (same connection as `AppDbContext` — no separate infra). Schema is the standard Quartz SQL Server DDL and must be applied per environment before first deploy. `UseProperties = true` forces all job parameters to be strings; this avoids pulling in Newtonsoft.Json. `System.Text.Json` is the serializer.

### Configuration / secrets

Secrets are loaded from **Azure Key Vault** outside Development (controlled by the `AzureKeyVaultUri` env var, with `DefaultAzureCredential`). `appsettings.json` contains `PLACEHOLDER` strings for production values — never commit real secrets. In Development, fill `appsettings.Development.json` (gitignored) or use `dotnet user-secrets`.

### Logging

Serilog is configured in `Program.cs` with **all sinks wrapped in `WriteTo.Async()`** (do not remove — sync sinks block request threads). `Destructurama.UsingAttributes()` is enabled so `[NotLogged]` and `[LogMasked]` on commands/DTOs prevent PII leaking into logs. Application Insights sink only activates when `ApplicationInsights:ConnectionString` is set.

OpenTelemetry registration is currently **commented out** in `Program.cs` due to CVE-2026-40894 / CVE-2026-42191 in the 1.15.2 packages — leave it commented until 1.15.3+ is published.

### Rate limiting

Two named policies: `"otp"` (sliding window, 3 req/hour, applied via `[EnableRateLimiting("otp")]` on the `SendOtp` action) and `"api"` (fixed window, 100 req/min per IP, general throttle).

## Conventions enforced by tooling

- **`Directory.Build.props`** turns on `Nullable=enable`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest`, deterministic + embedded debug symbols, for all non-test projects. Test projects opt out of `TreatWarningsAsErrors`. Don't add per-project overrides for these — change the props file if a global change is needed.
- **`.editorconfig`** mandates: file-scoped namespaces (warning), CRLF, UTF-8, 4-space indent, `var` only when type is apparent from RHS, language keywords (`int`, not `Int32`), required braces, `System.*` usings sorted first with a blank line separating groups, **comments in English only**.
- **`.cspell.json`** runs in CI on `src/**/*.cs` and `tests/**/*.cs`; if you introduce domain terms, add them to the `words` array rather than disabling the gate.
- `dotnet format --verify-no-changes` is a CI blocker — run `dotnet format` before pushing.

## Quality gates (CI)

PRs are blocked unless: backend line coverage ≥ 70%, zero `high`/`critical` vulnerable packages (`dotnet list package --vulnerable`), all NetArchTest architecture tests pass, and E2E-Staging critical paths pass.

## Test stack

xUnit + FluentAssertions + NSubstitute + Bogus for unit tests. For integration tests use Testcontainers.MsSql + Respawn (real SQL Server in Docker, fast reset between tests) — **do not mock the DbContext** for tests that exercise EF behavior. WireMock.Net mocks external APIs (1С, Kaspi Pay, SMS).
