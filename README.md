# ContactCenterAI

Plataforma SaaS de Soporte Inteligente para Contact Centers con enfoque RAG.

## Estructura

```text
src/backend/     Solución .NET 9 (Clean Architecture)
src/frontend/    SPA React + TypeScript + Vite + MUI
tests/           Proyectos de pruebas xUnit
deploy/docker/   Dockerfiles y scripts de base de datos
.github/workflows CI (restore, build, test, docker build)
```

## Requisitos

- .NET 9 SDK
- Node.js 22+
- Docker / Docker Compose

## Inicio rápido (local)

```bash
# Backend
dotnet restore src/backend/ContactCenterAI.sln
dotnet build src/backend/ContactCenterAI.sln
dotnet run --project src/backend/ContactCenterAI.Api

# Frontend
cd src/frontend/contact-center-web
cp .env.example .env
npm install
npm run dev
```

## Docker Compose

```bash
cp .env.example .env
docker compose up --build
```

Servicios:

| Servicio | URL |
|---|---|
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| Health | http://localhost:8080/health |
| Frontend | http://localhost:5173 |
| PostgreSQL + pgvector | localhost:5432 |

## Estado actual

Infraestructura base lista. Pendiente: entidades de dominio, CQRS, autenticación funcional, documentos, RAG y dashboard.
