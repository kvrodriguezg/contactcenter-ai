# ContactCenterAI

Plataforma SaaS de soporte para agentes de contact center con RAG sobre documentos PDF.

## Funcionalidades implementadas

- Autenticación con **Auth0** (JWT RS256) en entorno productivo/configurado
- Login JWT local solo para desarrollo (`AUTH_PROVIDER=Local`)
- Roles: **SuperAdmin**, **CompanyAdmin**, **Agent**
- Gestión multiempresa (tenancy por `CompanyId`)
- Usuarios y empresas
- Carga y listado de documentos PDF
- Worker Service: extracción PDF, chunks y embeddings
- Mensajería asíncrona con **RabbitMQ** (opcional por feature flag)
- Búsqueda semántica con **PostgreSQL + pgvector**
- **Chat API** independiente (conversaciones RAG con Gemini)
- **GraphQL BFF** para consultas agregadas
- Historial de conversaciones
- Tickets y escalamiento
- Despliegue en **AWS EC2** con Docker Compose
- HTTPS en borde con **Caddy** (snippet de reverse proxy)

## Stack técnico

| Capa | Tecnología |
|------|------------|
| Core API | ASP.NET Core 9, Clean Architecture, CQRS + MediatR |
| Chat API | ASP.NET Core 9 (bounded context propio) |
| GraphQL BFF | HotChocolate / BFF en `:8082` |
| Persistencia | PostgreSQL Core (pgvector) + PostgreSQL Chat |
| Worker | BackgroundService + consumers RabbitMQ |
| Frontend | React, Vite, TypeScript, Material UI |
| IA | Gemini API (embeddings y chat) |
| Mensajería | RabbitMQ |
| Infraestructura | Docker Compose, Caddy, GitHub Actions, AWS EC2 |

## Arquitectura de servicios

```text
Frontend (web)
    ├── Core API (:8080)     → db (PostgreSQL + pgvector)
    ├── Chat API (:8081)     → chat-db (PostgreSQL)
    ├── GraphQL BFF (:8082)  → Core API + Chat API
    └── Worker               → db + RabbitMQ + almacenamiento PDF
```

Detalle: [docs/architecture/](docs/architecture/) y [docs/README.md](docs/README.md).

## Requisitos

- .NET 9 SDK
- Node.js 22+
- Docker y Docker Compose
- Cuenta en [Google AI Studio](https://aistudio.google.com/) para `GEMINI_API_KEY`
- (Producción) Auth0 tenant + dominio Caddy/HTTPS

## Variables de entorno

```bash
cp .env.example .env
```

Variables relevantes (valores reales solo en `.env` local o en el host EC2; no se versionan):

- `AUTH_PROVIDER` / `VITE_AUTH_PROVIDER`: `Auth0` o `Local`
- `AUTH0_DOMAIN`, `AUTH0_AUDIENCE`, `VITE_AUTH0_*`
- `GEMINI_API_KEY`
- `MESSAGING_ENABLED`, `RABBITMQ_*`
- `CHAT_SERVICE_MODE` / `VITE_CHAT_SERVICE_MODE`: `Embedded` o `External`

Plantilla completa: `.env.example`.

## Ejecución local con Docker

```bash
cp .env.example .env
# Completar GEMINI_API_KEY y, si aplica, Auth0

docker compose up -d
docker compose ps
```

| Servicio | URL típica |
|----------|------------|
| Core API / Swagger | http://localhost:8080/swagger |
| Core health | http://localhost:8080/health |
| Chat API health | http://localhost:8081/health |
| GraphQL BFF | http://localhost:8082/graphql |
| Frontend | http://localhost:5173 |
| PostgreSQL Core | localhost:5432 |
| PostgreSQL Chat | localhost:5433 |
| RabbitMQ AMQP | localhost:5672 |

HTTPS en despliegue: ver `deploy/caddy/Caddyfile.graphql-bff.snippet`.

### Credenciales locales (solo desarrollo)

Válidas **únicamente** cuando `AUTH_PROVIDER=Local`. **No** corresponden al acceso productivo con Auth0.

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| admin@contactcenterai.cl | Admin123* | SuperAdmin |
| agente@contactcenterai.cl | Agent123* | Agent |

Con `AUTH_PROVIDER=Auth0`, `POST /api/auth/login` responde **410** y el acceso es vía Auth0.

## Pruebas

```bash
dotnet restore src/backend/ContactCenterAI.sln
dotnet build src/backend/ContactCenterAI.sln --configuration Release
dotnet test src/backend/ContactCenterAI.sln --configuration Release

cd src/frontend/contact-center-web
npm ci
npm run build
```

Evidencias de calidad: [docs/sumativa-2/](docs/sumativa-2/).

## CI/CD (GitHub Actions)

| Workflow | Función |
|----------|---------|
| `.github/workflows/ci.yml` | Restore, build, test, build frontend, build imágenes Docker |
| `.github/workflows/deploy.yml` | Despliegue a AWS EC2 por SSH (push a `main` / manual) |
| `.github/workflows/codeql.yml` | Análisis estático CodeQL (C# y JS/TS) |

Secrets de deploy (`EC2_HOST`, `EC2_USER`, `EC2_SSH_KEY`) solo en GitHub Secrets. El `.env` de producción permanece en la instancia EC2.

## Proveedor de IA

| Proveedor | Estado |
|-----------|--------|
| Gemini API | En uso (embeddings y chat) |
| Azure OpenAI / Bedrock | Descartados en el contexto académico del proyecto |

## Seguridad

- `.env` está en `.gitignore`.
- Roles y `CompanyId` se resuelven desde la base local, no desde claims de Auth0.
- Detalle: [docs/security/authentication-authorization.md](docs/security/authentication-authorization.md).
