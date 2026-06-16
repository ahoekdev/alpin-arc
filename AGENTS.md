# Project Layout

- `apps/api`: ASP.NET Core API.
- `apps/web`: Next.js web app.
- `docker-compose.yml`: local database services.

# Commands

- Run database: `docker compose up -d`
- Run API: `dotnet run --project apps/api/api.csproj --no-launch-profile`
- Run web app from `apps/web`: `npm run dev`
- Build web app from `apps/web`: `npm run build`

# API Guidance

- Follow existing ASP.NET Core controller, DTO, model, and EF Core patterns.
- Put API code under `apps/api`.
- Use migrations for schema changes.
- Create migrations with `dotnet ef migrations add <MigrationName>`.
- Apply migrations with `dotnet ef database update`.

# Next.js: ALWAYS read docs before coding

Before any Next.js work, find and read the relevant doc in `apps/web/node_modules/next/dist/docs/`. Your training data is outdated — the docs are the source of truth.

# Repo Guidance

- Keep changes scoped to the app being modified unless cross-app changes are required.
- Do not rewrite generated files unless the task requires it.
- Prefer existing project conventions over introducing new abstractions.
