## Purpose
- Short context for the auth service agent.

## Source of truth
- `team-hub-auth/` (`Program.cs`, `Controllers/AuthController*`, `Configuration/`, `Data/AuthDbContext.cs`, `appsettings*.json`, `.env*`)

## Do
- Endpoints: `register`, `login`, `refresh`, `logout`, `GET /health`.
- Flow: JWT + refresh-token cookie.
- Session storage: Redis (`Redis:ConnectionString`, prefix `auth:session:`).
- Health: `GET /health` checks PostgreSQL and Redis (`200` healthy, `503` unhealthy).
- Redis outage: `login` and `refresh` return `503` with JSON `error` when session store is unavailable.
- Lockout: 5 failed attempts = 15-min lockout.
- Validation:
  - `name`: 2-50 chars, Unicode letters, single space/’/-.
  - `surname`: 2-80 chars, Unicode letters, single space/’/-.
  - `username`: 3-30 chars, `^[a-zA-Z0-9._-]{3,30}$` (case-insensitive).
  - `password`: 12-128 chars.
- Cookies: `rememberMe` persistent vs session cookie behavior.
- Dev Env: Ports 5001/5002. Postgres (`localhost:5433`, db `auth`). Redis (`localhost:6379`). Container `team-hub-dev`.
- Prod Env (Docker): Host port 5001. Postgres (container `team-hub`, db `authdb`). Redis (`redis:6379`). Container `team-hub-auth-prod`.
- Integration tests: `team-hub-auth.Tests/Integration` (requires Docker; Testcontainers Redis and PostgreSQL).
- Keep this file updated after API, token, validation, or DB changes.

## Don't
- Modify API contracts without updating docs and tests.