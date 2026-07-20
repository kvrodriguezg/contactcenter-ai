# Manual Técnico — ContactCenterAI

**Proyecto:** ContactCenterAI  
**Tipo:** Plataforma SaaS multiempresa de soporte con RAG sobre documentos PDF  
**Versión del documento:** 1.0 (entrega final académica)  
**Alcance:** Documentación técnica alineada a la implementación real del repositorio  

---

## Tabla de contenidos

1. [Introducción](#1-introducción)
2. [Objetivo](#2-objetivo)
3. [Alcance](#3-alcance)
4. [Arquitectura](#4-arquitectura)
5. [Tecnologías](#5-tecnologías)
6. [Diagrama de componentes](#6-diagrama-de-componentes)
7. [Diagrama de despliegue](#7-diagrama-de-despliegue)
8. [Backend](#8-backend)
9. [Frontend](#9-frontend)
10. [GraphQL BFF](#10-graphql-bff)
11. [RabbitMQ](#11-rabbitmq)
12. [Worker](#12-worker)
13. [PostgreSQL](#13-postgresql)
14. [pgvector](#14-pgvector)
15. [Auth0](#15-auth0)
16. [Docker](#16-docker)
17. [AWS](#17-aws)
18. [GitHub Actions](#18-github-actions)
19. [Caddy](#19-caddy)
20. [Flujo RAG](#20-flujo-rag)
21. [Base de datos](#21-base-de-datos)
22. [Endpoints principales](#22-endpoints-principales)
23. [Variables de entorno](#23-variables-de-entorno)
24. [Instalación](#24-instalación)
25. [Compilación](#25-compilación)
26. [Despliegue](#26-despliegue)
27. [Logs](#27-logs)
28. [Troubleshooting](#28-troubleshooting)
29. [Mantenimiento](#29-mantenimiento)

---

## 1. Introducción

ContactCenterAI es una plataforma de soporte orientada a agentes de contact center. Permite cargar documentos PDF por empresa (tenant), indexarlos semánticamente y consultarlos mediante un asistente conversacional basado en **RAG** (*Retrieval-Augmented Generation*).

La solución está organizada en varios servicios .NET 9 y un frontend React (Vite), orquestados con Docker Compose. En producción se despliega en **AWS EC2**; la autenticación productiva usa **Auth0** (JWT RS256), mientras que el desarrollo admite JWT local (`AUTH_PROVIDER=Local`).

El proveedor de IA en uso es **Google Gemini API** (embeddings y chat). Azure OpenAI y Amazon Bedrock constan como descartados en el contexto académico del proyecto (ver README raíz).

---

## 2. Objetivo

Documentar de forma precisa:

- La arquitectura de microservicios / bounded contexts implementada.
- El stack tecnológico real (sin inventar componentes).
- Contratos HTTP/GraphQL, mensajería, persistencia y seguridad.
- Procedimientos de instalación, compilación, despliegue, operación y mantenimiento.

Este manual está dirigido a desarrolladores, evaluadores técnicos y personal de operaciones.

---

## 3. Alcance

### Incluido

| Área | Detalle |
|------|---------|
| Core API | Empresas, usuarios, documentos, búsqueda semántica, tickets, chat embebido (opcional) |
| Chat API | Conversaciones RAG en BD propia |
| GraphQL BFF | Agregación HotChocolate sobre Core + Chat |
| Worker | Procesamiento PDF → chunks → embeddings |
| Frontend | React + Vite + Material UI + Auth0 SPA |
| Infra | Docker Compose, RabbitMQ, PostgreSQL, pgvector, Caddy (snippet), CI/CD, AWS EC2 |

### Fuera de alcance

- Cambios de código o arquitectura.
- Proveedores de identidad distintos a Local/Auth0 (p. ej. Cognito no está en la implementación vigente).
- Proveedores de IA distintos a Gemini.

---

## 4. Arquitectura

### 4.1 Visión general

```text
Frontend (web :5173)
    ├── Core API (:8080)      → db (PostgreSQL 16 + pgvector)
    ├── Chat API (:8081)      → chat-db (PostgreSQL 16)
    ├── GraphQL BFF (:8082)   → Core API + Chat API (HTTP)
    └── Worker                → db + RabbitMQ + volumen de PDFs
```

### 4.2 Bounded contexts

| Contexto | Servicios | Datos |
|----------|-----------|-------|
| Core / Documents | Core API, Worker | `companies`, `users`, `documents`, `document_chunks`, `tickets`, (chat embebido opcional) |
| Chat / RAG | Chat API | `conversations`, `conversation_messages` en `chat-db` |
| BFF | GraphQL BFF | Sin base de datos propia |
| Presentación | Frontend | Tokens en cliente |

### 4.3 Feature flags relevantes

| Flag | Valores | Efecto |
|------|---------|--------|
| `AUTH_PROVIDER` / `VITE_AUTH_PROVIDER` | `Local` \| `Auth0` | Esquema de autenticación |
| `CHAT_SERVICE_MODE` / `VITE_CHAT_SERVICE_MODE` | `Embedded` \| `External` | Chat en Core vs Chat API |
| `MESSAGING_ENABLED` | `true` \| `false` | Publicación/consumo RabbitMQ |

### 4.4 Principios de diseño aplicados

- Clean Architecture + CQRS (MediatR) en Core y Chat.
- Autenticación (IdP/JWT) separada de autorización (perfil local en PostgreSQL).
- Tenancy por `CompanyId` resuelto en servidor, no confiado desde el cliente.
- Chat External no accede a la BD Core: usa HTTP (`/api/auth/me`, `/api/documents/search`).

Documentación de arquitectura existente: `docs/architecture/`.

---

## 5. Tecnologías

| Capa | Tecnología real |
|------|-----------------|
| Core API | ASP.NET Core 9, Clean Architecture, CQRS + MediatR, FluentValidation, EF Core 9, Serilog, Swashbuckle |
| Chat API | ASP.NET Core 9, MediatR, EF Core, Serilog |
| GraphQL BFF | HotChocolate (GraphQL), HTTP clients tipados |
| Worker | .NET Worker / BackgroundService |
| Frontend | React 19, Vite 6, TypeScript, Material UI 6, React Router 7, `@auth0/auth0-react` |
| Persistencia Core | PostgreSQL 16 + extensión `vector` (imagen `pgvector/pgvector:pg16`) |
| Persistencia Chat | PostgreSQL 16 Alpine (`postgres:16-alpine`) |
| IA | Gemini API (`gemini-embedding-001`, `gemini-2.5-flash`, dimensión 1536) |
| Mensajería | RabbitMQ 3 Management |
| Reverse proxy web | nginx 1.27 (contenedor `web`) |
| HTTPS borde | Caddy (snippet en `deploy/caddy/`) |
| Contenedores | Docker Compose |
| CI/CD | GitHub Actions (`ci.yml`, `deploy.yml`, `codeql.yml`) |
| Cloud | AWS EC2 (SSH deploy) |
| Identidad | Auth0 (prod) / JWT HS256 local (dev) |

---

## 6. Diagrama de componentes

```text
┌─────────────────────────────────────────────────────────────────────────┐
│                         Frontend (React + Vite)                          │
│  Login │ Dashboard │ Empresas │ Usuarios │ Documentos │ Chat │ Tickets   │
│  Auth0 SPA / JWT local │ GraphQL client │ REST clients                    │
└────────────┬───────────────────┬────────────────────┬───────────────────┘
             │ REST              │ /graphql           │ REST (External)
             ▼                   ▼                    ▼
      ┌─────────────┐     ┌─────────────┐      ┌─────────────┐
      │  Core API   │◄────│ GraphQL BFF │─────►│  Chat API   │
      │  :8080      │     │  :8082      │      │  :8081      │
      └──────┬──────┘     └─────────────┘      └──────┬──────┘
             │                                         │
             │                              HTTP me+search
             │◄────────────────────────────────────────┘
             ▼
      ┌─────────────┐   ┌──────────────┐   ┌─────────────┐
      │ db+pgvector │   │   RabbitMQ   │   │   chat-db   │
      └──────▲──────┘   └──────▲───────┘   └─────────────┘
             │                 │
      ┌──────┴─────────────────┴──────┐
      │            Worker              │
      │  PDF → chunks → embeddings     │
      └────────────────────────────────┘
             │
             ▼
      Gemini API (embeddings / chat)
```

---

## 7. Diagrama de despliegue

### 7.1 Local / Compose

| Contenedor | Imagen / build | Puerto host |
|------------|----------------|-------------|
| `contactcenterai-db` | `pgvector/pgvector:pg16` | 5432 |
| `contactcenterai-chat-db` | `postgres:16-alpine` | 5433 |
| `contactcenterai-api` | `Dockerfile.api` | 8080 |
| `contactcenterai-chat-api` | `Dockerfile.chat-api` | 8081 |
| `contactcenterai-bff` | `Dockerfile.bff` | 8082 |
| `contactcenterai-worker` | `Dockerfile.worker` | — |
| `contactcenterai-web` | `Dockerfile.web` (nginx) | 5173→80 |
| `contactcenterai-rabbitmq` | `rabbitmq:3-management` | 5672; UI 127.0.0.1:15672 |

Volúmenes: `postgres_data`, `chat_postgres_data`, `documents_data`, `*_logs`, `rabbitmq_data`.

### 7.2 Producción (AWS EC2)

```text
Internet → Caddy (HTTPS) → host EC2
              ├── /api/*      → Core :8080
              ├── /chat-api/* → Chat :8081
              ├── /graphql*   → BFF  :8082
              └── /           → Web  :5173

GitHub Actions (deploy.yml) → SSH → git pull → docker compose build/up
```

Secrets: `EC2_HOST`, `EC2_USER`, `EC2_SSH_KEY`. El `.env` de producción permanece en la instancia.

---

## 8. Backend

### 8.1 Solución

Ruta: `src/backend/ContactCenterAI.sln`

| Proyecto | Rol |
|----------|-----|
| `ContactCenterAI.Api` | Host Core REST |
| `ContactCenterAI.Application` | Casos de uso CQRS |
| `ContactCenterAI.Domain` | Entidades y reglas |
| `ContactCenterAI.Infrastructure` | EF, Gemini, RabbitMQ, storage, identidad |
| `ContactCenterAI.Worker` | Procesamiento de documentos |
| `ContactCenterAI.Chat.Api` | Host Chat REST |
| `ContactCenterAI.Chat.Application` | Casos de uso Chat |
| `ContactCenterAI.Chat.Domain` | Entidades Chat |
| `ContactCenterAI.Chat.Infrastructure` | EF Chat, clientes Core, Gemini chat |
| `ContactCenterAI.Bff` | GraphQL HotChocolate |

### 8.2 Capas Clean Architecture (Core)

1. **Api** — Controllers, middleware, Swagger, health.
2. **Application** — Commands/Queries, validadores, interfaces.
3. **Domain** — `Company`, `User`, `Document`, `DocumentChunk`, `Ticket`, etc.
4. **Infrastructure** — `ApplicationDbContext`, embeddings, mensajería, archivos.

### 8.3 Controladores Core

| Controller | Ruta base |
|------------|-----------|
| `AuthController` | `/api/auth` |
| `CompaniesController` | `/api/companies` |
| `UsersController` | `/api/users` |
| `DocumentsController` | `/api/documents` |
| `ChatController` | `/api/chat` |
| `TicketsController` | `/api/tickets` |

Health: `GET /health`.

### 8.4 Chat API

Ruta: `/api/chat` (ask, conversations).  
Propaga el Bearer token hacia Core para perfil y búsqueda semántica.

---

## 9. Frontend

Ruta: `src/frontend/contact-center-web`

| Aspecto | Detalle |
|---------|---------|
| Build | Vite 6 + TypeScript |
| UI | Material UI 6 |
| Routing | React Router 7 (`AppRouter`) |
| Auth | Local form o Auth0 (`authConfig.ts`, `AuthContext.tsx`) |
| Contenedor | nginx sirve `dist` y proxifica `/graphql` → `bff:8080` |

### Rutas UI

| Ruta | Acceso | Módulo |
|------|--------|--------|
| `/login` | Público (guest) | Login |
| `/dashboard` | Autenticado | Dashboard |
| `/companies` | SuperAdmin / CompanyAdmin | Empresas |
| `/users` | SuperAdmin / CompanyAdmin | Usuarios |
| `/company-summary` | Admin | Resumen GraphQL |
| `/documents` | Autenticado | Documentos PDF |
| `/chat` | Autenticado | Chat IA + historial |
| `/tickets` | Autenticado | Tickets |

---

## 10. GraphQL BFF

- Proyecto: `ContactCenterAI.Bff`
- Endpoint: `POST /graphql` (también Banana Cake Pop en desarrollo)
- Autorización: `[Authorize]` en `Query`
- Origen de datos: `ICoreApiClient` + `IChatApiClient` (HTTP, reenvío de Bearer)

### Consultas principales

| Campo | Origen |
|-------|--------|
| `me` | Core `/api/auth/me` |
| `companies` / `companyById` | Core |
| `users` / `userById` | Core |
| `documents` / `documentById` | Core |
| `tickets` / `ticketById` | Core |
| `conversations` / `conversationById` | Chat |

Extensiones de tipo cargan relaciones (usuarios de empresa, mensajes, etc.).  
Ejemplos: `docs/api/graphql-bff-examples.md`.

---

## 11. RabbitMQ

| Parámetro | Valor por defecto |
|-----------|-------------------|
| Imagen | `rabbitmq:3-management` |
| AMQP | Puerto 5672 |
| Management UI | `127.0.0.1:15672` (no exponer públicamente) |
| Exchange | `contactcenter.events` |
| Colas | `document.processing`, `ticket.escalation` |
| Feature flag | `Messaging__Enabled` / `MESSAGING_ENABLED` (default `false`) |

Con `MESSAGING_ENABLED=false`, el Worker usa **polling** de documentos pendientes en BD.  
Con `true`, se publican eventos y el Worker puede operar por consumidores; el polling puede quedar como reconciliación (`DOCUMENT_PROCESSING_POLLING_ENABLED`).

Credenciales `guest/guest` son solo para entorno local.

---

## 12. Worker

Proyecto: `ContactCenterAI.Worker` — clase `Worker : BackgroundService`.

### Ciclo

1. Lee `DocumentProcessing` + `Messaging`.
2. Si polling habilitado, en cada intervalo llama `IDocumentProcessingService.ProcessPendingDocumentsAsync`.
3. Procesa lotes de documentos pendientes: extracción PDF, chunking, embeddings Gemini, persistencia en `document_chunks`.

### Configuración típica (Compose)

| Variable | Default |
|----------|---------|
| `DocumentProcessing__IntervalSeconds` | 30 |
| `DocumentProcessing__ChunkSize` | 1000 |
| `DocumentProcessing__ChunkOverlap` | 150 |
| `DocumentProcessing__BatchSize` | 5 |
| `DocumentStorage__BasePath` | `/app/storage/documents` |

Comparte el volumen `documents_data` con Core API.

---

## 13. PostgreSQL

### Core (`db`)

- Imagen: `pgvector/pgvector:pg16`
- BD: `contactcenterai` (configurable)
- Init: `deploy/docker/init-db.sql` → `CREATE EXTENSION IF NOT EXISTS vector;`
- Migraciones: `ContactCenterAI.Infrastructure/Persistence/Migrations`

### Chat (`chat-db`)

- Imagen: `postgres:16-alpine`
- BD: `contactcenterai_chat`
- Puerto host: 5433
- Migraciones: `ContactCenterAI.Chat.Infrastructure/Persistence/Migrations`
- Sin requisito de pgvector

---

## 14. pgvector

- Extensión PostgreSQL `vector` en Core DB.
- Columna `document_chunks."Embedding"` tipo `vector(1536)`.
- Búsqueda semántica por distancia coseno (`<=>`) vía servicio de infraestructura.
- Dimensión alineada con `Gemini__EmbeddingDimensions` (default 1536).

Scripts de entrega: `scripts/create_extensions.sql`, `scripts/install_database.sql`.

---

## 15. Auth0

### Separación de responsabilidades

| Capa | Responsable |
|------|-------------|
| Autenticación | Auth0 (RS256) o JWT local (HS256) |
| Autorización | Tabla `users` (Role, CompanyId, IsActive) |

### Flujo Auth0

1. SPA: `loginWithRedirect` / `getAccessTokenSilently`.
2. API valida Authority `https://{AUTH0_DOMAIN}/`, audience exacta, lifetime.
3. Middleware resuelve usuario local por `ExternalSubject` (`sub`) o email.
4. `POST /api/auth/login` responde **410 Gone**.

### Flujo Local

1. `POST /api/auth/login` con email/password.
2. Emisión JWT HS256 (`Jwt__SecretKey`).
3. Roles y empresa siempre desde BD.

Detalle: `docs/architecture/auth0-integration.md`, `docs/security/authentication-authorization.md`.

---

## 16. Docker

### Dockerfiles (`deploy/docker/` y copia en `deployment/Docker/Dockerfiles/`)

| Archivo | Base build | Base runtime |
|---------|------------|--------------|
| `Dockerfile.api` | `dotnet/sdk:9.0` | `dotnet/aspnet:9.0` |
| `Dockerfile.chat-api` | sdk 9 | aspnet 9 |
| `Dockerfile.bff` | sdk 9 | aspnet 9 |
| `Dockerfile.worker` | sdk 9 | aspnet 9 / runtime worker |
| `Dockerfile.web` | `node:22-alpine` | `nginx:1.27-alpine` |

### Compose

- Raíz: `docker-compose.yml` (canónico).
- Entrega: `deployment/Docker/docker-compose.yml` y `docker-compose.prod.yml`.
- Guía: `deployment/Docker/deployment.md`.

---

## 17. AWS

Despliegue documentado e implementado vía GitHub Actions hacia **EC2**:

1. Runner SSH con `appleboy/ssh-action`.
2. Directorio esperado: `${HOME}/contactcenter-ai`.
3. Requiere `.git`, `docker-compose.yml` y `.env` en el host.
4. `git reset --hard origin/main` → `docker compose build` → `up -d`.
5. Health checks a Core y Chat en puertos del `.env`.

No se utiliza en este proyecto (implementación vigente): Azure App Service, ECS u otros orquestadores cloud distintos de Compose sobre EC2.

---

## 18. GitHub Actions

| Workflow | Archivo | Función |
|----------|---------|---------|
| CI | `.github/workflows/ci.yml` | Restore, build, test .NET; `npm ci` + build frontend; build imágenes API/Worker/Web |
| CD | `.github/workflows/deploy.yml` | Deploy SSH a EC2 en push a `main` o manual |
| CodeQL | `.github/workflows/codeql.yml` | Análisis estático C# y JS/TS |

CI usa .NET 9.0.x y Node.js 22.

---

## 19. Caddy

Archivo: `deploy/caddy/Caddyfile.graphql-bff.snippet`

Ejemplo de enrutamiento HTTPS:

```caddy
handle /api/* { reverse_proxy localhost:8080 }
handle /chat-api/* { uri strip_prefix /chat-api; reverse_proxy localhost:8081 }
handle /graphql* { reverse_proxy localhost:8082 }
handle { reverse_proxy localhost:5173 }
```

El contenedor `web` ya proxifica `/graphql` internamente vía nginx hacia el BFF.

---

## 20. Flujo RAG

### 20.1 Ingesta (offline / asíncrona)

```text
Usuario sube PDF → Core API guarda archivo + fila documents (Pending)
    → Worker detecta (polling o RabbitMQ)
    → Extrae texto → chunking (size/overlap)
    → Gemini embeddings (1536)
    → Persiste document_chunks + Status=Processed
```

### 20.2 Consulta (modo External / Chat API)

```text
Usuario pregunta en UI
  → POST /api/chat/ask (Chat API) + Bearer
  → GET Core /api/auth/me (perfil + CompanyId)
  → POST Core /api/documents/search (hits con Content)
  → Gemini Chat genera respuesta con contexto
  → Persiste conversation + messages (+ SourcesJson) en chat-db
  → UI muestra respuesta y fuentes
```

### 20.3 Modo Embedded

Con `CHAT_SERVICE_MODE=Embedded`, el frontend usa Core `:8080` para chat; los endpoints Chat de Core permanecen activos. En `External`, esos endpoints Core responden **410 Gone**.

### 20.4 Fallos controlados

| Condición | Código típico |
|-----------|---------------|
| Core caído (desde Chat) | 503 |
| Gemini no configurado / error IA | 502 |
| Token inválido | 401 |
| Sin contexto documental suficiente | Respuesta controlada sin inventar contenido |

---

## 21. Base de datos

### 21.1 Tablas Core

| Tabla | Propósito |
|-------|-----------|
| `companies` | Tenants |
| `users` | Perfil, rol, empresa, Auth0 `ExternalSubject` |
| `refresh_tokens` | Tokens locales |
| `documents` | Metadatos PDF y estado de procesamiento |
| `document_chunks` | Texto + embedding `vector(1536)` |
| `conversations` / `conversation_messages` | Chat embebido (Core) |
| `tickets` | Escalamiento / soporte |

### 21.2 Tablas Chat DB

| Tabla | Propósito |
|-------|-----------|
| `conversations` | `ExternalUserId`, `UserEmail`, `CompanyId`, `Title` |
| `conversation_messages` | `Role`, `Content`, `SourcesJson` |

### 21.3 Seed desarrollo

`ApplicationDbSeeder` (solo Development):

- Empresa: `Empresa Telecomunicaciones Simulada`
- `admin@contactcenterai.cl` / `Admin123*` → SuperAdmin
- `agente@contactcenterai.cl` / `Agent123*` → Agent

Script de entrega: `scripts/seed.sql`.

---

## 22. Endpoints principales

### Core API (`:8080`)

| Método | Ruta | Notas |
|--------|------|-------|
| POST | `/api/auth/login` | Local; 410 si Auth0 |
| GET | `/api/auth/me` | Perfil actual |
| GET/POST/PUT | `/api/companies` | CRUD + activate/deactivate |
| GET/POST/PUT | `/api/users` | Gestión usuarios |
| POST/GET | `/api/documents` | Upload y listado |
| GET | `/api/documents/{id}` | Detalle |
| POST | `/api/documents/search` | Búsqueda semántica |
| POST/GET | `/api/chat/*` | Chat embebido (según modo) |
| GET/POST/PUT | `/api/tickets` | Tickets y ciclo de vida |
| GET | `/health` | Health check |

### Chat API (`:8081`)

| Método | Ruta |
|--------|------|
| POST | `/api/chat/ask` |
| GET | `/api/chat/conversations` |
| GET | `/api/chat/conversations/{id}` |
| GET | `/health` |

### BFF (`:8082`)

| Método | Ruta |
|--------|------|
| POST | `/graphql` |
| GET | `/health` |

---

## 23. Variables de entorno

Plantilla: `.env.example`. No versionar `.env`.

### Base de datos

```bash
POSTGRES_DB=contactcenterai
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
CHAT_POSTGRES_DB=contactcenterai_chat
```

### Autenticación

```bash
AUTH_PROVIDER=Local          # o Auth0
AUTH0_DOMAIN=
AUTH0_AUDIENCE=https://contactcenterai-api
VITE_AUTH_PROVIDER=Local
VITE_AUTH0_DOMAIN=
VITE_AUTH0_CLIENT_ID=
VITE_AUTH0_AUDIENCE=https://contactcenterai-api
VITE_AUTH0_REDIRECT_URI=http://localhost:5173
```

### IA

```bash
AI_PROVIDER=Gemini
GEMINI_API_KEY=
GEMINI_EMBEDDINGS_MODEL=gemini-embedding-001
GEMINI_CHAT_MODEL=gemini-2.5-flash
GEMINI_EMBEDDING_DIMENSIONS=1536
```

### Chat / mensajería

```bash
CHAT_SERVICE_MODE=Embedded   # o External
VITE_CHAT_SERVICE_MODE=Embedded
VITE_CHAT_API_BASE_URL=http://localhost:8081
MESSAGING_ENABLED=false
RABBITMQ_HOST=rabbitmq
```

Listado completo en `.env.example` y `appsettings.json` de cada servicio.

---

## 24. Instalación

### Requisitos

- .NET 9 SDK
- Node.js 22+
- Docker + Docker Compose
- Cuenta Google AI Studio (`GEMINI_API_KEY`)
- (Prod) Tenant Auth0 + host EC2 + Caddy

### Pasos rápidos (Docker)

```bash
git clone <repo>
cd Proyecto_Final_IA
cp .env.example .env
# Editar GEMINI_API_KEY
docker compose up -d
docker compose ps
```

### Scripts SQL de entrega

```bash
# Extensión pgvector (Core)
psql -f scripts/create_extensions.sql

# Esquema de referencia (ver comentarios CORE vs CHAT)
psql -f scripts/install_database.sql

# Datos demo (Core)
psql -d contactcenterai -f scripts/seed.sql
```

En el flujo normal Docker + Development, las migraciones EF y el seeder sustituyen la ejecución manual.

---

## 25. Compilación

```bash
# Backend
dotnet restore src/backend/ContactCenterAI.sln
dotnet build src/backend/ContactCenterAI.sln --configuration Release
dotnet test src/backend/ContactCenterAI.sln --configuration Release

# Frontend
cd src/frontend/contact-center-web
npm ci
npm run build
```

Imágenes:

```bash
docker build -f deploy/docker/Dockerfile.api -t contactcenterai-api .
docker build -f deploy/docker/Dockerfile.chat-api -t contactcenterai-chat-api .
docker build -f deploy/docker/Dockerfile.bff -t contactcenterai-bff .
docker build -f deploy/docker/Dockerfile.worker -t contactcenterai-worker .
docker build -f deploy/docker/Dockerfile.web -t contactcenterai-web .
```

---

## 26. Despliegue

### Local

Ver sección 24 y `deployment/Docker/deployment.md`.

### AWS EC2 (automático)

1. Configurar secrets GitHub: `EC2_HOST`, `EC2_USER`, `EC2_SSH_KEY`.
2. Clonar repo en `${HOME}/contactcenter-ai` con `.env` de producción.
3. Push a `main` o ejecutar workflow `CD - Deploy AWS EC2`.
4. Verificar health Core/Chat en la instancia.

### HTTPS

Aplicar snippet Caddy en el host apuntando a los puertos Compose.

---

## 27. Logs

| Origen | Mecanismo |
|--------|-----------|
| Contenedores | `docker compose logs -f <servicio>` |
| Core / Chat / Worker / BFF | Serilog → consola + archivos rolling en `/app/logs` |
| Volúmenes | `api_logs`, `chat_api_logs`, `worker_logs`, `bff_logs` |

Nivel por defecto: Information (overrides Microsoft/EF en Warning).

**No registrar tokens** en logs de request.

---

## 28. Troubleshooting

| Problema | Causas frecuentes | Acción |
|----------|-------------------|--------|
| Contenedor `db` unhealthy | Credenciales, volumen corrupto | Logs `db`; recrear volumen solo si es aceptable perder datos |
| Documentos quedan Pending | Worker caído, sin `GEMINI_API_KEY`, messaging mal configurado | `docker compose logs worker` |
| Chat 503 | Core no responde | Health Core; red Compose |
| Chat 502 | Gemini / API key | Revisar `GEMINI_API_KEY` y modelos |
| Login 410 | Modo Auth0 | Usar botón Auth0; alinear `VITE_AUTH_PROVIDER` |
| 401 user_not_registered | Usuario Auth0 sin fila local | Crear usuario en `/users` con mismo email |
| GraphQL vacío / CORS | `WEB_ORIGIN` / token | Verificar CORS y Bearer |
| Build falla por RAM | Builds paralelos | Construir imágenes secuencialmente |
| Deploy SSH falla | Path o `.env` ausente en EC2 | Verificar `${HOME}/contactcenter-ai` |

---

## 29. Mantenimiento

### Rutinas recomendadas

1. **Backups** de volúmenes `postgres_data` y `chat_postgres_data`.
2. **Rotación** de `JWT_SECRET_KEY` y credenciales RabbitMQ en producción.
3. **Actualización** controlada: `git pull` → `docker compose build` → `up -d`.
4. **Monitoreo** de health endpoints y logs del Worker tras cargas masivas de PDF.
5. **Auth0**: mantener callback/logout URLs alineadas con el dominio público.
6. **Espacio en disco**: volumen `documents_data` crece con cada PDF.

### Rollback de autenticación

```bash
AUTH_PROVIDER=Local
VITE_AUTH_PROVIDER=Local
# rebuild/restart api + web
```

### Rollback de Chat External

```bash
CHAT_SERVICE_MODE=Embedded
VITE_CHAT_SERVICE_MODE=Embedded
```

### Independencia de servicios

- Detener `chat-api`: Core (login, empresas, documentos) sigue operativo.
- Detener `api`: Chat degrada (503 en ask / perfil).

---

## Referencias internas

| Documento | Ruta |
|-----------|------|
| README raíz | `README.md` |
| Índice docs | `docs/README.md` |
| Auth0 | `docs/architecture/auth0-integration.md` |
| Chat | `docs/architecture/chat-microservice.md` |
| Despliegue | `docs/architecture/microservices-deployment.md` |
| Seguridad | `docs/security/authentication-authorization.md` |
| GraphQL ejemplos | `docs/api/graphql-bff-examples.md` |
| Docker entrega | `deployment/Docker/deployment.md` |
| README final | `docs/final/README_Final.md` |
| Manual de usuario | `docs/final/Manual_Usuario.md` |

---

*Fin del Manual Técnico — ContactCenterAI*
