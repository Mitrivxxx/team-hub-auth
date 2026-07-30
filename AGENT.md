## Purpose
- Short context for the auth service agent.

## Source of truth
- `team-hub-auth/` (`Program.cs`, `Controllers/AuthController*`, `Configuration/`, `Data/AuthDbContext.cs`, `appsettings*.json`, `.env*`)
- `aspire/TeamHub.ServiceDefaults/Extensions.cs`

## Do
- Endpoints: `register`, `login`, `refresh`, `logout`, `change-password`, `GET users` (JWT), `GET /health`.
- Internal gRPC (not via gateway): `UserProfileService.GetUsersByIds` on port `5101` (dev) / `8081` (docker).
- API versioning: URL segment (`/api/auth/v0.0/*`), default version `0.0` (`Asp.Versioning.Mvc` 8.1.0).
- Flow: JWT + refresh-token cookie.
- Swagger (Development): Authorize button with Bearer JWT; paste access token (without `Bearer ` prefix) for `[Authorize]` endpoints like `GET users`.
- Session storage: Redis (`Redis:ConnectionString`, prefix `auth:session:`).
- Health: `GET /health` checks PostgreSQL and Redis (`200` healthy, `503` unhealthy).
- Docker healthcheck interval: `120s` (`docker-compose.yml` + `Dockerfile`).
- Kestrel: REST/health on `8080` (Http1AndHttp2), gRPC on `8081` (Http2 only).
- Shared contracts: `building-blocks/TeamHub.GrpcContracts` (`Protos/auth/v1/user_profile.proto`).
- Exclude `/health` and `/metrics` from Serilog request logging (`UseSerilogRequestLoggingExcludingHealth`).
- Observability via `TeamHub.Observability`: OTLP traces/logs, Prometheus `/metrics`, EF Core tracing.
- Keep `ExceptionMiddleware` as the first middleware (global try/catch; Serilog `Error` with stack trace; RFC 7807 `ProblemDetails`).
- Return `application/problem+json` with `correlationId` (and `sessionId` when present) in ProblemDetails extensions.
- In Development only: include `stackTrace` and exception message in ProblemDetails; in Production/Staging use generic detail (no stack trace in HTTP response).
- `RedisUnavailableException` → `503` ProblemDetails (authentication service temporarily unavailable).
- Lockout: Redis-based per-username limiter (keys `auth:login-attempts:` / `auth:login-lockout:`).
  - 5 failed attempts = 15-min lockout.
  - `POST /login`:
    - invalid credentials: `401` with `{ code: "AUTH_INVALID_CREDENTIALS", remainingAttempts }`
    - locked: `423` with `{ code: "AUTH_LOCKED", lockoutSeconds }`
- Validation:
  - `name`: 2-50 chars, Unicode letters, single space/’/-.
  - `surname`: 2-80 chars, Unicode letters, single space/’/-.
  - `username`: 3-30 chars, `^[a-zA-Z0-9._-]{3,30}$` (case-insensitive).
  - `email`: valid email address, 3-254 chars (case-insensitive uniqueness).
  - `password`: 12-128 chars.
- Cookies: `rememberMe` persistent vs session cookie behavior.
- Dev Env: HTTP on port `5001` + gRPC `5101` (`launchSettings.json`). Postgres (`localhost:5433`, db `auth_db`). Redis (`localhost:6379`). Container `team-hub-dev`.
- Prod Env (Docker): Host port 5001 (REST). Internal gRPC `8081`. Postgres (container `team-hub`, db `authdb`). Redis (`redis:6379`). Container `team-hub-auth-prod`. Connection string in auth `.env` (`ConnectionStrings__DefaultConnection`).
- Integration tests: `team-hub-auth.Tests/Integration` (requires Docker; Testcontainers Redis and PostgreSQL).
- Keep `CorrelationIdMiddleware` before authentication (`X-Correlation-ID` = OpenTelemetry `TraceId`; echo on response).
- Keep `SessionIdMiddleware` after `CorrelationIdMiddleware` (header `X-Session-ID`; fallback `Guid` when missing; echo in response).
- Keep `UserIdLoggingMiddleware` after `UseAuthentication` / `UseAuthorization` (JWT `sub` or `NameIdentifier` → `LogContext.UserId`).
- Keep `UseSerilogRequestLoggingExcludingHealth` after `UserIdLoggingMiddleware` so request logs include `TraceId`, `SpanId`, `CorrelationId`, `SessionId`, and `UserId`.
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