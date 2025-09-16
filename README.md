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

#### Prerequisites
- Docker Desktop installed and running
- Docker Compose v2.0+ (included with Docker Desktop)

#### Quick Start

##### Windows (PowerShell)
```powershell
# Navigate to project root
cd D:\aviater-code-test\mini-iam-api

# Build and start the container
docker-compose up --build

# Or run in detached mode (background)
docker-compose up --build -d

# View logs
docker-compose logs -f

# Stop the container
docker-compose down
```

##### Linux/macOS (Bash)
```bash
# Navigate to project root
cd /path/to/mini-iam-api

# Build and start the container
docker-compose up --build

# Or run in detached mode (background)
docker-compose up --build -d

# View logs
docker-compose logs -f

# Stop the container
docker-compose down
```

#### Access the Application
- **Swagger UI**: `http://localhost:3000/swagger`
- **Health Check**: `http://localhost:3000/health`
- **Detailed Health**: `http://localhost:3000/health/detailed`
- **API Base URL**: `http://localhost:3000`

#### Docker Compose Commands

```bash
# Build and start services
docker-compose up --build

# Start in background (detached mode)
docker-compose up -d

# View running containers
docker-compose ps

# View logs
docker-compose logs

# Follow logs in real-time
docker-compose logs -f

# Stop services
docker-compose down

# Stop and remove volumes
docker-compose down -v

# Rebuild without cache
docker-compose build --no-cache

# Restart services
docker-compose restart
```

#### Troubleshooting

##### Port Already in Use
If port 3000 is already in use, you can change it in `docker-compose.yml`:
```yaml
ports:
  - "3001:80"  # Change 3000 to 3001 or any available port
```

##### Container Won't Start
```bash
# Check container logs
docker-compose logs

# Check if port is available
netstat -an | grep :3000  # Windows
lsof -i :3000             # Linux/macOS

# Kill processes using the port (Windows)
netstat -ano | findstr :3000
taskkill /PID <PID> /F
```

##### Clean Docker Environment
```bash
# Remove all containers and images
docker-compose down --rmi all

# Remove unused Docker resources
docker system prune -a

# Rebuild from scratch
docker-compose up --build --force-recreate
```

##### Verify Installation
```bash
# Test health endpoint
curl http://localhost:3000/health

# Test with PowerShell (Windows)
Invoke-RestMethod -Uri "http://localhost:3000/health" -Method GET

# Test login endpoint
curl -X POST http://localhost:3000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@demo.com", "password": "Demo@321"}'
```

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
- `docker-compose.yml` exposes the API at `http://localhost:3000`.
- The container includes all necessary dependencies and runs the application in production mode.
- Health checks are configured to monitor application status.

## Configuration

### Environment Variables
The application can be configured using environment variables:

- `Jwt__Key` — HMAC key to sign JWTs (required)
- `Jwt__Issuer` — JWT issuer (default: "MiniIAM")
- `Jwt__Audience` — JWT audience (default: "MiniIAMClients")
- `Jwt__ExpireMinutes` — JWT expiration time in minutes (default: 20)
- `ASPNETCORE_ENVIRONMENT` — Environment (Development/Production)
- `ASPNETCORE_URLS` — URLs to bind to (default: "http://+:80")

### Docker Environment
The `docker-compose.yml` file includes default environment variables:
```yaml
environment:
  - Jwt__Key=your-super-secret-jwt-key-here-must-be-at-least-32-characters
  - Jwt__Issuer=MiniIAM
  - Jwt__Audience=MiniIAMClients
  - Jwt__ExpireMinutes=20
  - ASPNETCORE_ENVIRONMENT=Production
```

### Custom Configuration
To use your own configuration, create a `.env` file in the project root:
```env
JWT__KEY=your-custom-jwt-key-here
JWT__ISSUER=YourCompany
JWT__AUDIENCE=YourApp
JWT__EXPIRE_MINUTES=30
```

Then modify `docker-compose.yml` to use the `.env` file:
```yaml
services:
  miniiam-api:
    env_file:
      - .env
```

## Development vs Production

### Development Mode
- Uses EF Core InMemory database
- Swagger UI enabled
- Detailed error messages
- Hot reload support when running locally

### Production Mode (Docker)
- Uses EF Core InMemory database (can be configured for SQL Server)
- Swagger UI disabled
- Optimized for performance
- Health checks enabled
- Structured logging with Serilog

### Switching to SQL Server
To use SQL Server instead of InMemory database:

1. Update `docker-compose.yml`:
```yaml
services:
  miniiam-api:
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=MiniIAM;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;
    depends_on:
      - db
  
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
```

2. Update `WebApplicationExtensions.cs` to use SQL Server provider instead of InMemory.

## Recent Features

### Session Management
- **Session Entity**: Tracks user sessions with access/refresh tokens
- **Session Expiration**: Automatic cleanup of sessions older than 20 minutes
- **JWT Claims**: Session ID included in JWT tokens for tracking
- **Logout**: Proper session deactivation on logout

### Health Checks
- **Basic Health**: `/health` - Simple health status
- **Detailed Health**: `/health/detailed` - Comprehensive health information
- **Readiness**: `/health/ready` - Service readiness check
- **Liveness**: `/health/live` - Service liveness check

### Test Coverage
- **53 Tests**: Comprehensive test suite covering all endpoints
- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end API testing
- **Health Check Tests**: Dedicated health endpoint testing
- **Session Tests**: Session management functionality testing
