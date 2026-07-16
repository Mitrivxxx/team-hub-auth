## Purpose
- Short context for the auth service agent.

## Source of truth
- `team-hub-auth/` (`Program.cs`, `Controllers/AuthController*`, `Configuration/`, `Data/AuthDbContext.cs`, `appsettings*.json`, `.env*`)

## Do
- Endpoints: `register`, `login`, `refresh`, `logout`, `change-password`, `GET /health`.
- Flow: JWT + refresh-token cookie.
- Session storage: Redis (`Redis:ConnectionString`, prefix `auth:session:`).
- Health: `GET /health` checks PostgreSQL and Redis (`200` healthy, `503` unhealthy).
- Docker healthcheck interval: `120s` (`docker-compose.yml` + `Dockerfile`).
- Exclude `/health` and `/metrics` from Serilog request logging (`UseSerilogRequestLoggingExcludingHealth`).
- Observability via `TeamHub.Observability`: OTLP traces/logs, Prometheus `/metrics`, EF Core tracing.
- Keep `ExceptionMiddleware` as the first middleware (global try/catch; Serilog `Error` with stack trace; RFC 7807 `ProblemDetails`).
- Return `application/problem+json` with `correlationId` (and `sessionId` when present) in ProblemDetails extensions.
- In Development only: include `stackTrace` and exception message in ProblemDetails; in Production/Staging use generic detail (no stack trace in HTTP response).
- `RedisUnavailableException` → `503` ProblemDetails (authentication service temporarily unavailable).
- Lockout: 5 failed attempts = 15-min lockout.
- Validation:
  - `name`: 2-50 chars, Unicode letters, single space/’/-.
  - `surname`: 2-80 chars, Unicode letters, single space/’/-.
  - `username`: 3-30 chars, `^[a-zA-Z0-9._-]{3,30}$` (case-insensitive).
  - `password`: 12-128 chars.
- Cookies: `rememberMe` persistent vs session cookie behavior.
- Dev Env: Ports 5001/5002. Postgres (`localhost:5433`, db `auth`). Redis (`localhost:6379`). Container `team-hub-dev`.
- Prod Env (Docker): Host port 5001. Postgres (container `team-hub`, db `authdb`). Redis (`redis:6379`). Container `team-hub-auth-prod`. Connection string in auth `.env` (`ConnectionStrings__DefaultConnection`).
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
- Keep this file updated after API, token, validation, or DB changes.

## Don't
- Modify API contracts without updating docs and tests.