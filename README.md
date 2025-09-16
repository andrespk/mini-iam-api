# MiniIAM (.NET 9, Minimal API + CQRS)

This project implements a minimal IAM using **Minimal API** + **CQRS (MinimalCqrs)**, organized by layers:

```text
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
        LogInUser.cs
        LogOutUser.cs
      Users/
        AddUser.cs
        GetUser.cs
        AddUserRole.cs
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
docker-compose up --build
```

#### Linux/macOS (Bash)
```bash
docker-compose up --build
```

Browse `http://localhost:3000/swagger` or test health check at `http://localhost:3000/health`.

## Endpoints

### Authentication
- `POST /auth/login` — body: `{ "email": "", "password": "" }` → returns `{ accessToken, refreshToken }`.
- `POST /auth/logout` — header `Authorization: Bearer <token>`.

### Health Check
- `GET /health` — basic health check, returns `{ "status": "Healthy", "timestamp": "..." }`.
- `GET /health/detailed` — detailed health check with all registered checks and their status.
- `GET /health/ready` — readiness check, indicates if service is ready to accept requests.
- `GET /health/live` — liveness check, indicates if service is alive.

### Users
- `POST /users` — create a user (protected).
- `GET /users/{id}` — get user by ID (protected).
- `PUT /users/{id}/roles` — assign roles to user (protected).

All user endpoints require authentication via JWT token in the `Authorization` header.

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

### Run Specific Test Categories

#### Windows (PowerShell)
```powershell
# Run only unit tests
cd tests/MiniIAM.Tests
dotnet test --filter "Category=Unit" /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Run only integration tests
dotnet test --filter "Category=Integration" /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Run tests by namespace
dotnet test --filter "FullyQualifiedName~Unit" /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
dotnet test --filter "FullyQualifiedName~Integration" /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

#### Linux/macOS (Bash)
```bash
# Run only unit tests
cd tests/MiniIAM.Tests
dotnet test --filter "Category=Unit" /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Run only integration tests
dotnet test --filter "Category=Integration" /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Run tests by namespace
dotnet test --filter "FullyQualifiedName~Unit" /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
dotnet test --filter "FullyQualifiedName~Integration" /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

### Test Structure
- **MiniIAM.Tests**: Main test project containing both unit and integration tests
  - **Unit/**: Unit tests for individual components (AuthService, Validators, etc.)
  - **Integration/**: Integration tests for API endpoints and use cases

### Test Coverage
The provided tests exercise **API**, **Use Case**, and **Infrastructure** layers.
Current test coverage target: **≥85%**

#### Test Categories
- **Unit Tests**: Test individual components in isolation
  - `AuthServiceTests`: JWT token generation and validation
  - `LogInUserValidatorTests`: Input validation for login requests
- **Integration Tests**: Test API endpoints and use cases
  - `AuthAndUsersEndpointsTests`: Endpoint behavior and request/response validation
  - Tests for `GET /users/{id}`, `POST /users`, `PUT /users/{id}/roles`

## `.http` file
See `src/MiniIAM.Api/MiniIAM.Api.http` for ready-to-run endpoint calls.

### Example API Calls
```http
### Login
POST https://localhost:5001/auth/login
Content-Type: application/json

{
  "email": "admin@local",
  "password": "admin"
}

### Get User by ID
GET https://localhost:5001/users/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
Authorization: Bearer <your-jwt-token>

### Create User
POST https://localhost:5001/users
Authorization: Bearer <your-jwt-token>
Content-Type: application/json

{
  "email": "user@example.com",
  "name": "John Doe",
  "password": "password123",
  "byUserId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
}

### Assign Roles to User
PUT https://localhost:5001/users/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/roles
Authorization: Bearer <your-jwt-token>
Content-Type: application/json

{
  "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "roles": [
    {
      "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "name": "Admin",
      "users": null,
      "changesHistory": null
    }
  ],
  "byUserId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
}
```

## Docker

- `src/MiniIAM.Api/Dockerfile` builds & runs the API on ASP.NET 9.
- `docker-compose.yml` exposes the API at `8080`.

## Configuration
- `Jwt:Key` (or `Jwt__Key` in Docker) — HMAC key to sign JWTs.
