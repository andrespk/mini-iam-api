# MiniIAM (.NET 9, Minimal API + CQRS)

This project implements a minimal IAM using **Minimal API** + **CQRS (MinimalCqrs)**, organized by layers:

```
src/
  MiniIAM.Api/           # Endpoints (Auth, Users, Roles)
    Endpoints/
      AuthEndpoints.cs
      UsersEndpoints.cs
    Extensions/
      WebApplicationExtensions.cs
      WebApplicationExtensions.Cqrs.cs
    Swagger/
      DefaultResponsesOperationFilter.cs
    Program.cs
  MiniIAM.Application/    # Use cases (Commands/Queries)
    UseCases/
      Auth/
      Users/
  MiniIAM.Domain/         # Entities + DTOs
    Abstractions/
    Roles/
    Users/
  MiniIAM.Infrastructure/ # EF Core, Repositories, Auth, Caching
    Auth/
    Caching/
    Cqrs/
    Data/
    Extensions/
  MiniIAM.Shared/         # Extensions & utilities (AddMiniIamInfrastructure, UseMiniIamApi)
    Extensions/
    Middlewares/
tests/
  MiniIAM.Tests/          # Test project (Unit + Integration)
    Unit/                 # Unit tests
    Integration/          # Integration tests
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

### Windows (PowerShell)
```powershell
cd src
dotnet restore
dotnet build
dotnet run --project MiniIAM.Api
```

### Linux/macOS (Bash)
```bash
cd src
dotnet restore
dotnet build
dotnet run --project MiniIAM.Api
```

Swagger: `https://localhost:5001/swagger` (or whatever Kestrel port you see).

### Via Docker Compose

#### Windows (PowerShell)
```powershell
docker compose up --build
```

#### Linux/macOS (Bash)
```bash
docker compose up --build
```

Browse `http://localhost:8080/swagger`.

## Endpoints

- `POST /auth/login` — body: `{ "email": "", "password": "" }` → returns `{ accessToken, refreshToken }`.
- `POST /auth/logout` — header `Authorization: Bearer <token>`.
- `POST /users` — create a user (protected).
- `POST /users/{id}/roles` — assign a role (protected).

## Tests & Coverage

### Run All Tests

#### Windows (PowerShell)
```powershell
# Run all tests from solution root
cd src
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

#### Linux/macOS (Bash)
```bash
# Run all tests from solution root
cd src
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

### Run Specific Test Projects

#### Windows (PowerShell)
```powershell
# Unit tests only
cd tests/MiniIAM.Tests.Unit
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Integration tests only
cd ../MiniIAM.Tests.Integration
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Main test project (Unit + Integration)
cd ../MiniIAM.Tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

#### Linux/macOS (Bash)
```bash
# Unit tests only
cd tests/MiniIAM.Tests.Unit
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Integration tests only
cd ../MiniIAM.Tests.Integration
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Main test project (Unit + Integration)
cd ../MiniIAM.Tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

### Test Structure
- **MiniIAM.Tests**: Main test project containing both unit and integration tests
- **MiniIAM.Tests.Unit**: Dedicated unit tests project
- **MiniIAM.Tests.Integration**: Dedicated integration tests project

The provided tests exercise **API**, **Use Case**, and **Infrastructure** layers.
Extend them to easily reach **≥80% coverage** (the scaffolding and examples are ready).

## `.http` file
See `src/MiniIAM.Api/MiniIAM.Api.http` for ready-to-run endpoint calls.

## Docker

- `src/MiniIAM.Api/Dockerfile` builds & runs the API on ASP.NET 9.
- `docker-compose.yml` exposes the API at `8080`.

## Configuration
- `Jwt:Key` (or `Jwt__Key` in Docker) — HMAC key to sign JWTs.
