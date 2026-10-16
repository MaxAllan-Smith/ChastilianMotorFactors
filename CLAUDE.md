# ChastilianMotorFactors

.NET 10 ASP.NET Core Web API (controllers), Clean Architecture + SOLID. Work happens on `develop`; pushes to `master` deploy to Azure.

For language, build tooling and architecture, this file overrides the global RSPS/Kotlin/Gradle instructions: use C# and the `dotnet` CLI. The global rules on safety, documentation and protecting existing work still apply.

The project is in early scaffolding: no features, persistence or error-handling conventions exist yet. The first implementation of each sets the pattern; keep it simple and mention the choice in the summary.

## Architecture (mandatory)

Layers and allowed references:

| Layer | Project | May reference | Owns |
|---|---|---|---|
| Domain | `ChastilianMotorFactors.Domain` | nothing (no NuGet packages) | Entities, value objects, enums, domain rules and invariants |
| Application | `ChastilianMotorFactors.Application` | Domain | Use cases, application DTOs, input validation, interfaces for persistence and external services |
| Infrastructure | `ChastilianMotorFactors.Infrastructure` | Application | Implementations of Application interfaces (database, external APIs, files, email) |
| Presentation | `ChastilianMotorFactors.Api` | Application, Infrastructure | Controllers, API request/response contracts, middleware, `Program.cs` composition root |

- Dependencies point inward only. Never add a reference that breaks the table above.
- Business rules live in Domain (invariants) or Application (workflows), never in controllers or Infrastructure.
- Api references Infrastructure only for `AddInfrastructure(...)` in `Program.cs`. Controllers use Application types only.
- Each layer registers its own services in its `DependencyInjection.cs` (`AddApplication()`, `AddInfrastructure(configuration)`); `Program.cs` only calls these.
- Domain entities never cross the API boundary; controllers map to and from API contracts / application DTOs.
- An interface belongs in Application only when Infrastructure implements it, or when there is a real second implementation or test seam. Don't add interfaces, repositories, mediators or base classes speculatively.
- New NuGet packages, projects or layers need a clear reason; ask first for anything architectural.

## Code organisation and style

- Group by feature within each layer (e.g. `Application/Vehicles/`, `Api/Controllers/VehiclesController.cs`), not by technical type across the project.
- File-scoped namespaces that match the folder path; one public type per file.
- Constructor injection only.
- Prefer modern idiomatic C# (records for DTOs/contracts, `required`/init properties, pattern matching) where it aids readability; readable over clever.

## Implementation guidelines

- **Async:** async end to end, accept and pass `CancellationToken` from the controller action down; never `.Result`, `.Wait()` or `async void`.
- **Nullability:** fix nullable warnings properly; don't suppress them with `!` without a guarantee.
- **Validation:** validate all client input server-side; reject invalid requests with `400`.
- **Errors:** use exceptions for unexpected failures, not control flow. Return `ProblemDetails` with consistent status codes (`400` validation, `404` not found, `409` conflict). Never leak stack traces or internals in responses.
- **Logging:** use injected `ILogger<T>` with structured message templates; never log secrets or personal data. Serilog is the provider, configured only in `Program.cs` (bootstrap logger, `AddSerilog`, `UseSerilogRequestLogging`). Levels and sinks live in the `Serilog` section of `appsettings*.json`, not `Logging`. Other layers must not reference Serilog.
- **API design:** RESTful routes, plural resource names, correct verbs and status codes (`201` + location for creates). Don't change existing routes or contracts without being asked; that breaks clients.
- **Persistence (when added):** lives in Infrastructure only; any migrations are generated and named deliberately, never edited after being applied.

## Security and configuration

- No secrets in `appsettings*.json` or source. Use user-secrets locally and Azure App Service configuration in production.
- Bind settings with the options pattern in the layer that uses them.
- Don't edit `.github/workflows/` or the Azure service-dependency files in `ChastilianMotorFactors.Api/Properties/` (`ServiceDependencies/`, `serviceDependencies*.json`) unless asked.

## Workflow

1. Make the smallest change that fully solves the task, in the correct layer, reusing existing code rather than duplicating it. No unrelated refactors, renames or formatting changes.
2. Bug fixes: find the root cause, fix it there, add a regression test.
3. New features: Domain → Application → Infrastructure → Api, including only the layers the feature needs.

## Build and test

- While iterating, build only the affected project, e.g. `dotnet build ChastilianMotorFactors.Application -v q`
- Before finishing a code change, build the whole solution: `dotnet build ChastilianMotorFactors.slnx -v q`
- Tests: NUnit, in `ChastilianMotorFactors.Tests` (solution folder `/test/`; reference the layer projects it tests). Run `dotnet test ChastilianMotorFactors.slnx`. Test Domain and Application logic first. CI does not run these tests yet.
- On failure: fix the cause and re-run. Don't weaken tests, suppress warnings or skip checks to get green. If something can't be fixed, report it with the relevant error lines.
- Never claim a build or test passed unless it ran and succeeded in this session. Documentation-only changes don't need a build.
- Visual Studio may rewrite `ChastilianMotorFactors.slnx` while it is open. After `dotnet sln` changes, re-read the file to confirm.

## Token efficiency

- Act directly on clear requests. Ask only when an ambiguity would change the correct implementation.
- Read only the files the task touches; use targeted `Grep`/`Glob`, not broad exploration. Don't re-read unchanged files.
- Keep command output short (filter or tail long output).
- No web research unless the answer depends on external information. No subagents or written plans for routine tasks.

## Completion report

Keep it brief: what changed, which files, which verification ran and its result, and any limitations or decisions the user should know about. Leave out sections that don't apply.
