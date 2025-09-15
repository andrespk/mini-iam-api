# MiniIAM (.NET 9, Minimal API + CQRS)

Este projeto implementa um **IAM** mínimo com **Minimal API** + **CQRS (MinimalCqrs)**, organizado por camadas:

```
src/
  MiniIAM.Api/           # Endpoints (Auth, Users, Roles)
    Endpoints/
      AuthEndpoints.cs
      UsersEndpoints.cs
      RolesEndpoints.cs
    Program.cs
  MiniIAM.Application/    # Use cases (Commands/Queries)
  MiniIAM.Domain/         # Entidades + DTOs
  MiniIAM.Infrastructure/ # EF Core, Repositories, Auth, Caching
  MiniIAM.Shared/         # Extensões e utilidades (AddMiniIamInfrastructure, UseMiniIamApi)
  MiniIAM.Tests/          # Testes (unitários e de integração)
```

## Padrões e decisões
- **Minimal APIs** com `MapGroup` e proteção por JWT: todas as rotas são protegidas por padrão,
  exceto `POST /auth/login` e `POST /auth/logout` (permitidas anonimamente).
- **CQRS** com `MinimalCqrs` (`IMediator.Send`) para acoplar endpoints a _use cases_ em `MiniIAM.Application`.
- **EF Core InMemory** para dev/test: definido em `AddMiniIamInfrastructure` (pode ser trocado depois para SQL Server).
- **Swagger/OpenAPI** ativado no `Development`.
- **Testes** com xUnit, FluentAssertions e WebApplicationFactory.

## Como rodar localmente
Pré-requisitos: .NET 9 SDK

```bash
cd src
dotnet restore
dotnet build
dotnet run --project MiniIAM.Api
```

A API sobe em `http://localhost:5000` (Kestrel default) ou `https://localhost:5001`. No Docker, usa `:8080`.

### Via Docker Compose
```bash
docker compose up --build
```
Acesse `http://localhost:8080/swagger`.

## Endpoints

- `POST /auth/login` — corpo: `{ "email": "", "password": "" }` → retorna `{ accessToken, refreshToken }`.
- `POST /auth/logout` — header `Authorization: Bearer <token>`.
- `POST /users` — cria usuário (protegido).
- `PUT /users/{id}` — atualiza usuário (protegido).
- `POST /users/{id}/roles/{roleId}` — adiciona role (protegido).
- `POST /roles` — cria role (protegido).
- `PUT /roles/{id}` — atualiza role (protegido).

## Testes & Cobertura

Executar testes:
```bash
cd src/MiniIAM.Tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

Os testes fornecidos cobrem autenticação básica e _handlers_ essenciais. Amplie para atingir **≥85%** conforme evoluir os casos.

## Arquivo `.http`
No `MiniIAM.Api/MiniIAM.Api.http` há exemplos para chamar os endpoints com `rest-client`/VS Code.

## Docker

- `MiniIAM.Api/Dockerfile` faz _restore_, _build_ e _publish_ e roda no ASP.NET 9.
- `docker-compose.yml` expõe a API em `8080`.

## Variáveis
- `Jwt:Key` (ou `Jwt__Key` no Docker) — chave HMAC para assinar JWT.
