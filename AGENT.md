## Purpose
- Short context for the auth service agent.

## Source of truth
- `team-hub-auth/Program.cs`
- `team-hub-auth/Controllers/AuthController*.cs`
- `team-hub-auth/Configuration/*`
- `team-hub-auth/Data/AuthDbContext.cs`
- `team-hub-auth/appsettings*.json`
- `team-hub-auth/.env` / `.env.example` (secrets per service; local dev and docker compose)

## Do
- Keep endpoints: `register`, `login`, `refresh`, `logout`.
- Keep health endpoint: `GET /health`.
- Keep refresh-token cookie and JWT flow aligned with controllers.
- Store refresh sessions in Redis (`Redis:ConnectionString`, key prefix `auth:session:`).
- Keep login `rememberMe` behavior aligned across `login` and `refresh` (persistent vs session cookie).
- Keep login lockout: 5 failed attempts, 15-minute lockout.
- Keep register validation strict:
  - `name`: trim, 2-50, Unicode letters + single space/apostrophe/hyphen.
  - `surname`: trim, 2-80, Unicode letters + single space/apostrophe/hyphen.
  - `username`: trim, `^[a-zA-Z0-9._-]{3,30}$`, unique case-insensitive.
  - `password`: length 12-128.
- In dev, keep profiles on `http://localhost:5001` and `https://localhost:5002`.
- In dev, connect to postgres via `appsettings.Development.json` (`localhost:5433`, database `auth`, container `team-hub-dev` from `docker-compose.dev.yml`).
- In dev, connect to Redis via `appsettings.Development.json` (`localhost:6379`, container `team-hub-redis-dev` from `docker-compose.dev.yml`).
- In docker (Production), map auth to host `5001` (`team-hub-auth-prod`, `appsettings.Production.json`, database `authdb` on container `team-hub`, Redis on `redis:6379`).
- Update this file after API, token, validation, or DB changes.

## Don't
- Do not change the API contract without updating docs and tests.
