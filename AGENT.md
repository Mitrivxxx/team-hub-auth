## Purpose
- Short context for the auth service agent.

## Source of truth
- `team-hub-auth/` (`Program.cs`, `Controllers/AuthController*`, `Controllers/MeController.cs`, `Configuration/`, `Seeding/`, `Data/AuthDbContext.cs`, `appsettings*.json`, `.env*`)
- `aspire/TeamHub.ServiceDefaults/Extensions.cs`

## Do
- Endpoints: `register`, `login`, `refresh`, `logout`, `change-password` (public identity reset), `GET/PATCH /me` (JWT), `POST /me/change-password` (JWT, current + new password), `PUT|DELETE /me/avatar` (JWT), `GET users` (JWT, paginated, optional search), `GET /health`.
- `GET/PATCH /me`: current user profile; PATCH `{ name?, surname?, email? }` (at least one field); username is read-only; `409` on duplicate email. `UserResponse.avatarUrl` is a SAS URL (nullable).
- `PUT|DELETE /me/avatar`: JPEG/PNG/WebP, max 2 MB; blob path `users/{userId}/avatar.{ext}`; `503` when blob storage is not configured. Production requires `BlobStorage:ConnectionString`.
- `GET users?page=&pageSize=&q=`: defaults `page=1`, `pageSize=50`; `pageSize` clamped to max `100`. Optional `q` filters by case-insensitive substring on `Name`, `Surname`, or `Email`; multi-word `q` (e.g. `Jan Kowalski`) requires each token to match across those fields.
- Indexes on `users`: unique `Username`, unique filtered `Email`, `Name`, `Surname` (search), unique `RefreshTokenHash`.
- Demo seed (`Seeding/`): run with `--seed` when `ASPNETCORE_ENVIRONMENT` is `Development` or `Staging` and `Seed:Enabled=true` (migrates, seeds, exits without hosting API). Production is blocked.
  - Config: `Seed` in `appsettings.{Environment}.json` (`UserCount` Development=50, Staging=10 000). Do not log passwords.
  - Active login user: username `JanWilk123`, password `janwilk123` / `Seed:ActivePassword` (Argon2; JWT/refresh on login — not pre-seeded). Email `janwilk123@teamhub.local`.
  - Bulk users (`TeamHub.DemoSeed`): username `Name+Surname+123`, password and email local-part = lowercase username (same rule as active user). Bogus PL/EN names; reserved active username excluded. Collision suffix before `123` when needed.
  - Layout: `Seeding/DemoDataSeeder` + helpers `Seeding/Users/ActiveDemoUserSeeder` + `BulkDemoUserSeeder`. Volumes from `appsettings.{Environment}.json` (`UserCount`). Domain/persistence stay in `Data/`.
  - Idempotent: skips active user if username exists; skips bulk when other `@teamhub.local` emails exist.
  - Seed organization after auth (org resolves users via auth gRPC); Aspire then seeds notification inbox.
  - Aspire one-shot: `cd aspire/TeamHub.AppHost && dotnet run -- --seed` (`seed-auth` → `seed-auth-api` → `seed-organization` → `seed-notification`; see `aspire/AGENT.md`).
- Internal gRPC (not via gateway): `UserProfileService.GetUsersByIds` + `ResolveUsers` on port `5101` (dev) / `8081` (docker).
- API versioning: URL segment (`/api/auth/v0.0/*`), default version `0.0` (`Asp.Versioning.Mvc` 8.1.0).
- Flow: JWT + refresh-token cookie.
- Swagger (Development): Authorize button with Bearer JWT; paste access token (without `Bearer ` prefix) for `[Authorize]` endpoints like `GET users` and `/me`.
- Session storage: Redis (`Redis:ConnectionString`, prefix `auth:session:`).
- Health: `GET /health` checks PostgreSQL and Redis (`200` healthy, `503` unhealthy).
- Docker healthcheck interval: `120s` (`docker-compose.yml` + `Dockerfile`).
- Kestrel: REST/health on `8080` (Http1AndHttp2), gRPC on `8081` (Http2 only).
- Shared contracts: `building-blocks/TeamHub.GrpcContracts` (`Protos/auth/v1/user_profile.proto`).
- Exclude `/health` and `/metrics` from Serilog request logging (`UseSerilogRequestLoggingExcludingHealth` from `TeamHub.Observability`).
- Observability via `TeamHub.Observability`: OTLP traces/logs, Prometheus `/metrics`, EF Core tracing, shared Exception/CorrelationId/UserIdLogging middleware.
- Keep `UseTeamHubExceptionHandling` as the first middleware (global try/catch; Serilog `Error` with stack trace; RFC 9457 `ProblemDetails`).
- Register `AddTeamHubProblemDetails()` for ModelState / FluentValidation `ValidationProblemDetails`.
- Return `application/problem+json` with stable `type` URIs (`https://teamhub.dev/problems/...`), `correlationId` (and `sessionId` when present).
- In Development only: include `stackTrace` and exception message in ProblemDetails; in Production/Staging use generic detail (no stack trace in HTTP response).
- `RedisUnavailableException` → `503` ProblemDetails (`type` = `…/service-unavailable`) via `RedisUnavailableExceptionMapper` (`AddTeamHubExceptionMapper`).
- Lockout: Redis-based per-username limiter (keys `auth:login-attempts:` / `auth:login-lockout:`).
  - 5 failed attempts = 15-min lockout.
  - `POST /login` errors are RFC 9457 ProblemDetails:
    - invalid credentials: `401` `type=…/invalid-credentials` with extensions `code=AUTH_INVALID_CREDENTIALS`, `remainingAttempts`
    - locked: `423` `type=…/account-locked` with extensions `code=AUTH_LOCKED`, `lockoutSeconds`
- Error catalog: `docs/errors.mb`.
- Validation:
  - `name`: 2-50 chars, Unicode letters, single space/’/-.
  - `surname`: 2-80 chars, Unicode letters, single space/’/-.
  - `username`: 3-30 chars, `^[a-zA-Z0-9._-]{3,30}$` (case-insensitive).
  - `email`: valid email address, 3-254 chars (case-insensitive uniqueness).
  - `password`: 8-128 chars.
- Cookies: `rememberMe` persistent vs session cookie behavior.
- Dev Env: HTTP on port `5001` + gRPC `5101` (`launchSettings.json`). Postgres (`localhost:5433`, db `auth_db`). Redis (`localhost:6379`). Container `team-hub-dev`.
- Local `.env` (from `.env.example`): optional for standalone `dotnet run` outside Aspire. Under Aspire, AppHost injects `ConnectionStrings` / Redis / Jwt — DotNetEnv uses `NoClobber` so those win over `.env` (do not expect fixed Postgres `5433`).
- Prod Env (Docker): Host port 5001 (REST). Internal gRPC `8081`. Postgres (container `db-postgres`, db `authdb`). Redis (`cache-redis:6379`). Container `srv-auth-prod`. Connection string in auth `.env` (`ConnectionStrings__DefaultConnection`); compose also sets `Redis__ConnectionString=cache-redis:6379`.
- Integration tests: `team-hub-auth.Tests/Integration` (requires Docker; Testcontainers Redis and PostgreSQL).
- Keep `UseTeamHubCorrelationId` before authentication (`X-Correlation-ID` = OpenTelemetry `TraceId`; echo on response).
- Keep `SessionIdMiddleware` after CorrelationId (header `X-Session-ID`; fallback `Guid` when missing; echo in response).
- Keep `UseTeamHubUserIdLogging` after `UseAuthentication` / `UseAuthorization` (JWT `sub` or `NameIdentifier` → `LogContext.UserId`).
- Keep `UseSerilogRequestLoggingExcludingHealth` after UserIdLogging so request logs include `TraceId`, `SpanId`, `CorrelationId`, `SessionId`, and `UserId`.
- Dev log template: `[{Level:u3}] [{TraceId}] [{SpanId}] [{CorrelationId}] [{SessionId}] [{UserId}] ...` (no `{Timestamp}` — Loki adds its own).
- Prod logs: Serilog compact JSON + OTLP sink to collector; structured fields `TraceId`, `SpanId`, `CorrelationId`, `SessionId`, `UserId`.
- Cookie-only endpoints (`login`, `register`, `refresh`, `logout`) have empty `UserId` unless `Authorization: Bearer` is sent.
- Enrich all request logs with Serilog `CorrelationId` via `LogContext`.
- Echo `X-Correlation-ID` on every response.
- Keep this file updated after API, token, validation, DB, or gRPC contract changes.

## CI (GitHub Actions)
- Workflow: `.github/workflows/ci.yml`.
- Branches: `stage` (tests only), `main` (tests + GHCR image push). `dev` has no CI.
- PRs targeting `stage` or `main` run tests before merge.
- Shared deps: dual `actions/checkout` — monorepo `Mitrivxxx/team-hub@main` then overlay this repo at `services/team-hub-auth`.
- Image: `ghcr.io/<owner>/team-hub-auth` (`latest` + short commit SHA on `main` push). Uses `GITHUB_TOKEN` (no extra secrets).
- Integration tests use Testcontainers (Docker required on runner).

## Don't
- Modify API contracts without updating docs and tests.