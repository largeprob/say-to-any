# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

`src/backend` is the .NET 10 backend for SqlBoTx, an NL2SQL agent system. It exposes REST APIs, persists semantic modeling data in SQL Server, stores chat history, streams AI agent output to the frontend using AG-UI/SSE, and uses Qdrant plus OpenAI-compatible clients for semantic/vector workflows.

## Commands

Run commands from `src/backend` unless noted otherwise:

```bash
dotnet restore SqlBoTx.slnx
dotnet build SqlBoTx.slnx
dotnet run --project SqlBoTx.Net.ApiService
dotnet run --project SqlBoTx.Net.AppHost     # Aspire host: ApiService + DbManager
dotnet run --project SqlBoTx.Net.DbManager   # DB migration/seeding utility
```

Entity Framework commands use `SqlBoTx.Net.DbManager` as the migrations project and `SqlBoTx.Net.EFCore` as the DbContext project:

```bash
dotnet ef migrations add <Name> --project SqlBoTx.Net.DbManager --startup-project SqlBoTx.Net.DbManager --context SqlBotxDBContext
dotnet ef database update --project SqlBoTx.Net.DbManager --startup-project SqlBoTx.Net.DbManager --context SqlBotxDBContext
```

There are no test projects in `SqlBoTx.slnx` currently. There is also no lint/format script; use `dotnet build SqlBoTx.slnx` as the primary validation command.

## Solution Structure

`SqlBoTx.slnx` contains the production backend projects:

- `SqlBoTx.Net.AppHost` — .NET Aspire distributed app host; starts `ApiService` and `DbManager`.
- `SqlBoTx.Net.ApiService` — ASP.NET Core API entry point and controllers.
- `SqlBoTx.Net.Application` — application services, auth/token logic, background task services, vector services, and ToSQL agent orchestration.
- `SqlBoTx.Net.Application.Contracts` — service interfaces, DTOs, and vector model contracts.
- `SqlBoTx.Net.Domain` — domain entities, repository interfaces, and domain managers for business validation.
- `SqlBoTx.Net.Domain.Share` — shared enums, constants, and value-object infrastructure.
- `SqlBoTx.Net.EFCore` — `SqlBotxDBContext`, EF mappings, repositories, unit of work, pagination helpers, transaction helpers.
- `SqlBoTx.Net.Infrastructure` — AG-UI event/SSE infrastructure, system tools, and agent context providers.
- `SqlBoTx.Net.Core` — shared API/controller helpers, exception handling, JWT setup, Semantic Kernel/OpenAI registration, agent/tool abstractions.
- `SqlBoTx.Net.Share` — cross-layer helpers, exceptions, configuration, pagination request base types, JSON options.
- `SqlBoTx.Net.ServiceDefaults` — Aspire service defaults, health endpoints, OpenTelemetry, resilience, service discovery.
- `SqlBoTx.Net.DbManager` — EF migration host, seeding/initialization service, and development-only `/reset-db` endpoint.

`SqlBoTx.Net.Web` exists in the tree but is not included in `SqlBoTx.slnx`; avoid treating it as part of the active backend unless it is explicitly added.

## Runtime Wiring

`SqlBoTx.Net.ApiService/Program.cs` is the composition root:

1. Adds Aspire service defaults, ProblemDetails, global exception handling, validation, controllers, OpenAPI/Swagger.
2. Registers domain managers via `AddDomainManagers()`.
3. Registers EF Core and repositories via `AddEFCore(configuration)` using the `OneDataAgent` connection string.
4. Configures Wolverine with SQL Server persistence, EF Core transactions, and durable local queues.
5. Registers application services with `builder.AddApplicationService()`.
6. Starts Quartz hosted scheduling and schedules enabled background tasks on startup.
7. Registers infrastructure services, JWT authentication, CORS, health/default endpoints, and controllers.

## DDD Layering Rules

Before coding, plan the change using DDD layering. Identify the aggregate or feature being changed, then decide which responsibilities belong in each project. Keep business rules in the domain/application layers and keep transport, persistence, and infrastructure details out of domain code.

### What belongs in each layer

- **ApiService**: HTTP-only concerns — controllers, route definitions, request binding, authorization attributes, response status codes, OpenAPI metadata, and delegating to application services. Do not put business rules, EF queries, SQL, vector calls, or agent orchestration directly in controllers.
- **Application.Contracts**: Public application boundary — service interfaces, request/response DTOs, page/query DTOs, vector models, and contracts shared by API/frontend-facing features. DTO validation attributes belong here.
- **Application**: Logic and transaction orchestration — application services coordinate use-case flow, transaction boundaries, DTO/domain mapping, domain manager calls, repository calls, external services, background task services, vector services, and agent orchestration. Do not inject or use `DbContext` in this layer; all database reads/writes must go through repository interfaces and be implemented in `EFCore`. Do not put EF mapping, raw SQL, LINQ-to-EF query details, or controller-specific response logic here.
- **Domain**: Core business model — aggregate entities, value objects, repository interfaces, and domain managers that enforce business invariants such as uniqueness, existence checks, status transitions, and initialization of domain fields. Domain managers may query repositories for validation but must not call `SaveChanges`, publish HTTP responses, or depend on EF Core implementations.
- **Domain.Share**: Cross-domain domain concepts — enums, constants, value-object base types, and domain-level shared definitions that have no dependency on application, EF, API, or infrastructure concerns.
- **EFCore**: Persistence implementation — `SqlBotxDBContext`, EF entity configurations/mappings, repository implementations, unit of work, transaction proxies, and query/pagination extensions. Do not define business rules here beyond persistence constraints and query mechanics.
- **Infrastructure**: Technical integrations — AG-UI protocol events/SSE helpers, agent tools, context providers, external protocol adapters, and integration glue that is not a core business rule. Keep reusable business decisions out of this layer.
- **Core**: Framework-level shared building blocks — base controller/API response helpers, global exception handling, JWT/Semantic Kernel/OpenAI startup extensions, base agent/tool abstractions. Avoid feature-specific business logic here.
- **Share**: General cross-cutting primitives — exceptions, generic helpers, security helpers, configuration objects, JSON options, and pagination request base types. Avoid adding feature-specific behavior.
- **DbManager**: Database lifecycle — EF migrations host, seed data, initialization/reset endpoints. Treat reset/delete operations as destructive.
- **ServiceDefaults/AppHost**: Aspire hosting and service defaults only — health checks, telemetry, service discovery, distributed startup wiring.

### Planning checklist for new features

1. Define the aggregate/feature and the use cases.
2. Add or update DTOs and service interfaces in `Application.Contracts`.
3. Put invariant checks and domain initialization in a `Domain/<Aggregate>/XxxManager` when the feature mutates an aggregate.
4. Add repository interface methods in `Domain` only when the domain/application layer needs a new query boundary.
5. Implement persistence details in `EFCore/Repositorys` and mapping changes in EFCore configuration/context.
6. Implement orchestration in `Application/<Feature>/XxxService`, including DTO mapping, manager calls, repository calls, and unit-of-work boundaries; keep every database operation behind repository interfaces and never use `DbContext` directly here.
7. Expose the use case through a thin `ApiService/Controllers/XxxController` action.
8. Register new managers, repositories, and services in the existing `DomainExtensions`, `EFCoreExtensions`, and `ApplicationExtensions` methods.

## Data Access and Domain Pattern

- EF Core uses `SqlBotxDBContext` in `SqlBoTx.Net.EFCore`. The context declares DbSets for semantic metadata, organizations/auth, chat threads/messages, AI models, and background tasks.
- `SqlBotxDBContext.OnModelCreating` dynamically applies base table/property mapping for DbSets, applies EFCore assembly configurations, and maps Wolverine envelope storage.
- Repositories live in `SqlBoTx.Net.EFCore/Repositorys/` and implement interfaces from `SqlBoTx.Net.Domain`.
- Domain managers live next to their aggregate in `SqlBoTx.Net.Domain/<Aggregate>/` and handle business validation/initialization only; they do not save changes.
- Application services live in `SqlBoTx.Net.Application/<Feature>/`, call managers/repositories, map DTOs with Mapster, and expose contracts from `SqlBoTx.Net.Application.Contracts`.
- Use existing pagination helpers (`PageQuery`, `PagedResult<T>`, `ToPagedListAsync`) and dynamic `WhereIf...` filters rather than hand-rolling pagination.

## API Pattern

- Controllers live in `SqlBoTx.Net.ApiService/Controllers/` and normally inherit `LarApi` except specialized controllers such as `ChatController`.
- Feature controllers route at `[Route("[controller]")]`, use Chinese XML summaries/tags, and declare `[ProducesResponseType]` for success responses.
- Mutating endpoints generally return `NoContent()` with HTTP 204; query endpoints return `Ok(data)`.
- DTOs in contracts should use nullable required properties plus `[Required(ErrorMessage = "{0}不能为空")]` and `[DisplayName]`, matching the project validation convention.

## NL2SQL / AG-UI Flow

- `ChatController.ToSQLCompletion` accepts `RunAgentInput`, calls `IToSQLAgentService.RunAsync`, and returns `AGUIServerSentEventsResult` for SSE streaming.
- `ToSQLAgentService` creates or reuses chat threads, builds Microsoft Agents.AI agents from the configured OpenAI-compatible chat client, stores chat history through `EfCoreChatHistoryProvider`, and streams `BaseEvent` AG-UI events.
- AG-UI event types, JSON conversion, and SSE result handling live in `SqlBoTx.Net.Infrastructure/AGUI/`.
- Agent context providers such as `MainAgentConstraintContextProvider`, `MemoryProvider`, and `SessionSpecificGuidanceContextProvider` supply system constraints, memory, and frontend tool guidance.
- Vector search uses `QdrantVectorService` and a keyed OpenAI-compatible embedding client; collection names and vector constants are under `SqlBoTx.Net.Domain.Share/Constants/Vectors/`.

## Database Management

- `DbManager` configures `SqlBotxDBContext` with migrations in the DbManager assembly.
- In development, `DbManager` exposes `/reset-db`, which deletes and reinitializes the database. Treat it as destructive and ask before using it.
- Do not read or edit `SqlBoTx.Net.DbManager/Migrations/` unless explicitly asked; project rules mark it as a forbidden read directory.

## Configuration Notes

- Required runtime configuration includes the `OneDataAgent` SQL Server connection string, JWT settings, CORS settings, Qdrant connectivity, and OpenAI-compatible model credentials/endpoints.
- Some client registrations are currently hardcoded in startup extension code; do not copy secrets into documentation, logs, commits, or new configuration examples.
- `ApiService` enables Swagger/OpenAPI only in development and maps Swagger UI to `/swagger` using `/openapi/v1.json`.
