# ChastilianMotorFactors

.NET 10 ASP.NET Core Web API (controllers), Clean Architecture + SOLID. Branching follows git-flow (see below); pushes to `master` deploy to Azure.

For language, build tooling and architecture, this file overrides the global RSPS/Kotlin/Gradle instructions: use C# and the `dotnet` CLI. The global rules on safety, documentation and protecting existing work still apply.

The project is in early scaffolding: logging (Serilog), the mediator (Cortex.Mediator) and EF Core are wired up, but no features, entities or error-handling conventions exist yet. The first implementation of each sets the pattern; keep it simple and mention the choice in the summary.

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
- An interface belongs in Application only when Infrastructure implements it, or when there is a real second implementation or test seam. Don't add interfaces, repositories or base classes speculatively.
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
- **Mediator:** use Cortex.Mediator, never MediatR (no longer free). Controllers inject `IMediator` and send commands (`ICommand<T>` + `ICommandHandler<,>`) or queries (`IQuery<T>` + `IQueryHandler<,>`). Requests and their handlers live together in the Application feature folder. `AddApplication()` scans the Application assembly, so handlers need no manual registration. No pipeline behaviors are registered yet.
- **Persistence:** EF Core with SQL Server, in Infrastructure only. `Infrastructure/Persistence/ApplicationDbContext.cs` applies every `IEntityTypeConfiguration<T>` in the assembly; add one per entity in `Persistence/Configurations/`. The connection string is `ConnectionStrings:Database`: Development uses LocalDB via `appsettings.Development.json`, and production must set it in Azure App Service configuration or startup fails. Generate and name migrations deliberately, and never edit them after they're applied:
  `dotnet ef migrations add <Name> --project ChastilianMotorFactors.Infrastructure --startup-project ChastilianMotorFactors.Api --output-dir Persistence/Migrations`

## Security and configuration

- No secrets in `appsettings*.json` or source. Use user-secrets locally and Azure App Service configuration in production.
- Bind settings with the options pattern in the layer that uses them.
- Don't edit `.github/workflows/` or the Azure service-dependency files in `ChastilianMotorFactors.Api/Properties/` (`ServiceDependencies/`, `serviceDependencies*.json`) unless asked.

## Git flow (mandatory)

The repo uses git-flow (AVH edition), initialised with `master` (production, deploys to Azure), `develop` (integration) and the default prefixes (`feature/`, `bugfix/`, `release/`, `hotfix/`, `support/`). Always use `git flow` commands for branch lifecycle; never create, merge or delete these branches with plain `git branch`/`checkout -b`/`merge`.

| Work | Start | Finish |
|---|---|---|
| New feature | `git flow feature start <name>` | `git flow feature finish <name>` |
| Non-urgent fix (from `develop`) | `git flow bugfix start <name>` | `git flow bugfix finish <name>` |
| Release | `git flow release start <version>` | `git flow release finish <version>` |
| Urgent production fix (from `master`) | `git flow hotfix start <version>` | `git flow hotfix finish <version>` |

- Branch names: short kebab-case (e.g. `feature/serilog-logging`); release/hotfix names are versions (e.g. `1.2.0`).
- Never commit directly to `master` or `develop`. Before starting work, check the current branch; if on `develop` with no feature branch, start one.
- `develop` and `master` are protected on GitHub: changes reach them only through a pull request (no approvals needed, but review threads must be resolved). Direct pushes are rejected. **Never run `git flow <type> finish` for feature or bugfix branches.** It merges locally and deletes the remote branch, then the push to `develop` is rejected, leaving the work only on the local machine.
- Completing a feature or bugfix branch, once the build and tests pass locally:
  1. Commit the changes on the branch.
  2. Push the branch: `git flow <type> publish <name>` the first time, then `git push`.
  3. Open a PR into `develop`: `gh pr create --base develop --fill`.
  4. Wait for CI to pass (`gh pr checks <number> --watch`) and resolve any review threads, then merge: `gh pr merge <number> --merge --delete-branch`. Use a merge commit to keep the git-flow history.
  5. Sync locally: `git checkout develop`, `git pull --ff-only`, then `git branch -d <type>/<name>` if the branch still exists.
- Releases and hotfixes go to `master`, which deploys to Azure, so they also go through PRs. They happen only when explicitly asked, as does tagging.
- Weekly release: `.github/workflows/weekly-release-pr.yml` runs every Sunday at 20:00 UTC (or manually from the Actions tab). If `develop` is ahead of `master` and no such PR is open, it opens a `develop` → `master` PR. A person merges it, with a merge commit; never merge it automatically. It depends on the repo setting "Allow GitHub Actions to create and approve pull requests". Scheduled workflows only run from `master`, the default branch.

## Workflow

1. Make the smallest change that fully solves the task, in the correct layer, reusing existing code rather than duplicating it. No unrelated refactors, renames or formatting changes.
2. Bug fixes: find the root cause, fix it there, add a regression test.
3. New features: Domain → Application → Infrastructure → Api, including only the layers the feature needs.

## Build and test

- While iterating, build only the affected project, e.g. `dotnet build ChastilianMotorFactors.Application -v q`
- Before finishing a code change, build the whole solution: `dotnet build ChastilianMotorFactors.slnx -v q`
- Tests: NUnit, in `ChastilianMotorFactors.Tests` (solution folder `/test/`; reference the layer projects it tests). Run `dotnet test ChastilianMotorFactors.slnx`. Test Domain and Application logic first.
- CI (`.github/workflows/ci.yml`) runs on pushes to `develop`, `feature/**`, `bugfix/**`, `release/**` and `hotfix/**`, and on PRs to `develop`/`master`. It restores, fails on vulnerable NuGet packages (including transitive ones), builds Release and runs the tests. Deployment (`ChastilianMotorFactorsApi.yml`) runs on pushes to `master`: it builds and tests the whole solution, then publishes the Api to Azure. A failing test blocks the deploy.
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
