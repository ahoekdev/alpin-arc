## Commands

Run application: `dotnet run --launch-profile https`

## Database

Run database `docker compose up -d`

Inspect database: `docker exec -it db psql -U postgres -d alpinarc`

## Migrations

Create migration `dotnet ef migrations add <MigrationName>`

Update database `dotnet ef database update`
