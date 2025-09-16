# MiniIAM (.NET 9, Minimal API + CQRS)

This project implements a minimal IAM using **Minimal API** + **CQRS (MinimalCqrs)**, organized by layers:

```
src/
  MiniIAM.Api/           # Endpoints (Auth, Users, Roles)
    Endpoints/
      AuthEndpoints.cs
      UsersEndpoints.cs
      RolesEndpoints.cs
    Program.cs
  MiniIAM.Application/    # Use cases (Commands/Queries)
  MiniIAM.Domain/         # Entities + DTOs
  MiniIAM.Infrastructure/ # EF Core, Repositories, Auth, Caching
  MiniIAM.Shared/         # Extensions & utilities (AddMiniIamInfrastructure, UseMiniIamApi)
tests/
  MiniIAM.Tests.Unit/     # Unit tests
  MiniIAM.Tests.Integration/ # Integration tests
```

## Patterns & decisions
- **Minimal APIs** with `MapGroup` and JWT protection: all routes are protected by default,
  except `POST /auth/login` and `POST /auth/logout` (anonymous allowed).
- **CQRS** using `MinimalCqrs` (`ICommandDispatcher.Dispatch`) to connect endpoints with **use cases** in `MiniIAM.Application`.
- **EF Core InMemory** for dev/test: configured in `AddMiniIamInfrastructure` (you can later switch to SQL Server).
- **Swagger/OpenAPI** enabled in `Development`.
- **Tests** with xUnit, FluentAssertions and WebApplicationFactory.

## Run locally
Prerequisites: .NET 9 SDK

```bash
cd src
dotnet restore
dotnet build
dotnet run --project MiniIAM.Api
```

Swagger: `https://localhost:5001/swagger` (or whatever Kestrel port you see).

### Via Docker Compose
```bash
docker compose up --build
```
Browse `http://localhost:8080/swagger`.

## Endpoints

- `POST /auth/login` — body: `{ "email": "", "password": "" }` → returns `{ accessToken, refreshToken }`.
- `POST /auth/logout` — header `Authorization: Bearer <token>`.
- `POST /users` — create a user (protected).
- `PUT /users/{id}` — update a user (protected).
- `POST /users/{id}/roles/{roleId}` — assign a role (protected).
- `POST /roles` — create a role (protected).
- `PUT /roles/{id}` — update a role (protected).

## Tests & Coverage

Run tests:
```bash
cd tests/MiniIAM.Tests.Unit
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

cd ../MiniIAM.Tests.Integration
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

The provided tests exercise **API**, **Use Case**, and **Infrastructure** layers.
Extend them to easily reach **≥80% coverage** (the scaffolding and examples are ready).

## `.http` file
See `src/MiniIAM.Api/MiniIAM.Api.http` for ready-to-run endpoint calls.

## Docker

- `src/MiniIAM.Api/Dockerfile` builds & runs the API on ASP.NET 9.
- `docker-compose.yml` exposes the API at `8080`.

## Configuration
- `Jwt:Key` (or `Jwt__Key` in Docker) — HMAC key to sign JWTs.
