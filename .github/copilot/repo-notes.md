# BaustellenBob - Workspace Notes

## Tech Stack
- .NET 8.0, Blazor Server (Interactive Server), MudBlazor 9.1.0
- EF Core 8.x + Npgsql + BCrypt.Net-Next 4.1.0 + QuestPDF 2024.12.3
- PostgreSQL, Docker, Railway deployment

## MudBlazor 9.x Gotchas
- `ShowMessageBox` → renamed to `ShowMessageBoxAsync`
- `MudTextField` requires explicit `T="string"` type parameter
- No `Name` attribute on `MudTextField` (use native HTML inputs for form POST)
- No `Title` attribute on `MudIconButton` (use `aria-label`)

## Project Structure
- 5 projects: Domain, Application, Infrastructure, Server, Shared
- Services live in Infrastructure (not Application) to avoid circular refs
- Multitenancy via EF Core HasQueryFilter + Claims-based TenantProvider

## User DB Config
- Local PostgreSQL password: "root" (user-modified from default "postgres")
