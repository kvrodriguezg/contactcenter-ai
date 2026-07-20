# ContactCenterAI — Architecture & Coordination Report

> **Author:** Subagent 1 — Análisis y Coordinación
> **Mode:** READ-ONLY architecture analysis. No functional source code was modified. No git operations were performed. This markdown file is the only artifact written.
> **Base commit analyzed:** `3b894da` ("Merge branch 'develop'") on branch `feature/final-architecture`.
> **Scope:** Coordinates 5 feature subagents — (2) admin companies/users, (3) multitenancy, (4) tickets, (5) rabbitmq/messaging, (6) graphql bff.

---

## 0. Executive Summary

- The system is a **Clean Architecture / CQRS** ASP.NET Core 9 solution with **two bounded contexts**: the Core stack (`ContactCenterAI.*` — API, Application, Domain, Infrastructure, Worker) and the Chat microservice stack (`ContactCenterAI.Chat.*`). Each has its **own PostgreSQL database** and its **own `ApplicationDbContext` / `ChatDbContext`**.
- **Multitenancy already partially exists**: `CompanyId` columns are present on `User` (nullable), `Document` (required), Core `Conversation` (required), and Chat `Conversation` (required). Tenant isolation is **application-enforced** in MediatR handlers via `ICurrentUserService.CompanyId`, resolved from the **local `users` table** (not JWT claims). There is **no global EF query filter** yet — this is exactly what feature (3) multitenancy should add.
- **Companies and Users pages/endpoints already exist but are list-only** (read-only). Feature (2) admin extends them to full CRUD.
- **Tickets do not exist anywhere** (no entity, table, migration, endpoint, page). Feature (4) is greenfield but touches the shared DbContext + migrations + frontend routing.
- **No message queue exists**: the Worker uses **30s DB polling**. Feature (5) rabbitmq introduces `IEventPublisher` + RabbitMQ consumer.
- **No GraphQL BFF exists**. Feature (6) adds a new service + docker-compose entry.
- **Highest conflict risk: EF Core migrations** — admin, multitenancy, and tickets will each generate migrations against the **same `ApplicationDbContext`**. A single migration owner / serialized migration strategy is mandatory.

---

## 1. Domain Entities

### 1.1 Core domain (`src/backend/ContactCenterAI.Domain`)

Base types:
- `Common/BaseEntity.cs` — `Id : Guid`.
- `Common/AuditableEntity.cs` — `CreatedAt : DateTime`, `UpdatedAt : DateTime?` (extends `BaseEntity`).

All entities are plain POCOs (no constructors/factory methods).

| Entity | File | Key fields | `CompanyId`? |
|---|---|---|---|
| **Company** | `Tenancy/Company.cs` (7–18) | `Name`, `Status : CompanyStatus`, nav `Users`, `Documents`, `Conversations` | Tenant **root** (no CompanyId on itself) |
| **User** | `Identity/User.cs` (7–32) | `CompanyId : Guid?`, `Company?`, `Email`, `PasswordHash`, `Role : Role`, `IsActive`, `ExternalSubject : string?`, `AuthenticationProvider`, `LastLoginAt`, navs `RefreshTokens`, `UploadedDocuments`, `Conversations` | **Yes (nullable)** |
| **RefreshToken** | `Identity/RefreshToken.cs` (5–22) | `UserId`, `Token`, `ExpiresAt`, `RevokedAt`, computed `IsExpired/IsRevoked/IsActive` | No |
| **Document** | `Documents/Document.cs` (7–34) | `CompanyId : Guid` (required), `UploadedByUserId`, `FileName`, `OriginalFileName`, `ContentType`, `SizeBytes`, `StoragePath`, `Status : DocumentStatus`, `ProcessedAt`, `ErrorMessage`, nav `Chunks` | **Yes (required)** |
| **DocumentChunk** | `Documents/DocumentChunk.cs` (5–22) | `DocumentId`, `ChunkIndex`, `Content`, `Embedding : float[]?`, `EmbeddingModel`, `EmbeddedAt`, `CreatedAt` | No (via Document) |
| **Conversation** (Core) | `Chat/Conversation.cs` (7–20) | `CompanyId : Guid` (required), `UserId`, `Title`, nav `Messages` | **Yes (required)** |
| **ConversationMessage** (Core) | `Chat/ConversationMessage.cs` (5–18) | `ConversationId`, `Role : MessageRole`, `Content`, `SourcesJson`, `CreatedAt` | No (via Conversation) |

Enums:
- `Tenancy/CompanyStatus.cs`: `Active = 1`, `Inactive = 2`.
- `Identity/Role.cs`: `SuperAdmin = 1`, `CompanyAdmin = 2`, `Agent = 3`.
- `Identity/AuthenticationProvider.cs`: `Local = 0`, `Auth0 = 1`.
- `Documents/DocumentStatus.cs`: `Uploaded = 1`, `PendingProcessing = 2`, `Processing = 3`, `Processed = 4`, `Failed = 5`.
- `Chat/MessageRole.cs`: `User = 0`, `Assistant = 1`, `System = 2`.

### 1.2 Chat domain (`src/backend/ContactCenterAI.Chat.Domain`)

Separate assembly, no shared base classes, **no FK to Core DB**:
- **Conversation** (`Conversation.cs` 3–20): `Id`, `ExternalUserId : Guid` (Core user id, denormalized), `UserEmail`, `CompanyId : Guid` (required), `Title`, timestamps, nav `Messages`.
- **ConversationMessage** (`ConversationMessage.cs` 3–18): `ConversationId`, `Role`, `Content`, `SourcesJson`, `CreatedAt`.
- **MessageRole** (`MessageRole.cs`): same values as Core.

### 1.3 Key findings for planning

- **`Ticket` entity does not exist** — greenfield for feature (4).
- **`CompanyId` already exists broadly** — feature (3) multitenancy should focus on a **global query filter / `ITenantEntity` marker + `ICurrentUserService.CompanyId` enforcement**, NOT on adding columns to existing entities (they already have them). Any new tenant entity (e.g. `Ticket`) should carry `CompanyId : Guid` from the start.
- `User.CompanyId` is **nullable** (SuperAdmin has null) — the tenant filter must tolerate null-company SuperAdmins.

---

## 2. DbContext(s)

### 2.1 `ApplicationDbContext` (Core) — `src/backend/ContactCenterAI.Infrastructure/Persistence/ApplicationDbContext.cs`
- **DbSets (17–29):** `Companies`, `Users`, `RefreshTokens`, `Documents`, `DocumentChunks`, `Conversations`, `ConversationMessages`.
- **`OnModelCreating` (31–36):** `HasPostgresExtension("vector")` + `ApplyConfigurationsFromAssembly(...)`.
- **Configurations** in `Persistence/Configurations/`: `CompanyConfiguration`, `UserConfiguration`, `RefreshTokenConfiguration`, `DocumentConfiguration`, `DocumentChunkConfiguration`, `ConversationConfiguration`, `ConversationMessageConfiguration`. All map to snake-ish table names (`companies`, `users`, `refresh_tokens`, `documents`, `document_chunks`, `conversations`, `conversation_messages`) and store enums via `HasConversion<string>()`.
- **pgvector:** `DocumentChunk.Embedding` → `vector(1536)` (`DocumentChunkConfiguration.cs` 11, 29–40, with `float[]? ↔ Pgvector.Vector` conversion + `ValueComparer`).
- **Npgsql registration:** `Infrastructure/DependencyInjection.cs` 63–68 — `UseNpgsql(connectionString, o => o.UseVector())`. Connection: `ConnectionStrings:DefaultConnection` → compose `db` service (`pgvector/pgvector:pg16`).

### 2.2 `ChatDbContext` (Chat) — `src/backend/ContactCenterAI.Chat.Infrastructure/Persistence/ChatDbContext.cs`
- **DbSets (13–15):** `Conversations`, `ConversationMessages`.
- **Inline `OnModelCreating` config** (no separate configuration classes). Tables `conversations` / `conversation_messages`; indexes on `CompanyId`, `ExternalUserId`, composite `(CompanyId, ExternalUserId)`. `Role` stored as string (max 50). **No pgvector.**
- **Npgsql:** `Chat.Infrastructure/DependencyInjection.cs` 46–51 — plain `UseNpgsql` (no `UseVector`). Connection: `ConnectionStrings:ChatDatabase` → compose `chat-db` (`postgres:16-alpine`, port 5433).

### 2.3 `AuthTestDbContext` (tests only) — `tests/ContactCenterAI.Infrastructure.Tests/Identity/AuthTestDbContext.cs`
Mirrors Core DbSets but simplified/no pgvector; ignores several entities. Not shipped.

> **Note:** Both databases contain tables literally named `conversations` and `conversation_messages` but with **different schemas** (Core uses `UserId`; Chat uses `ExternalUserId` + `UserEmail`). Do not confuse the two.

---

## 3. EF Core Migrations

### 3.1 ApplicationDbContext migrations (`Infrastructure/Persistence/Migrations/`)
1. `20260708034733_InitialIdentity` — pgvector extension; `companies`, `users` (CompanyId nullable, FK Restrict, unique Email), `refresh_tokens`.
2. `20260709033955_AddDocuments` — `documents` (CompanyId, UploadedByUserId, FKs Restrict), `document_chunks`.
3. `20260709194636_AddDocumentChunkEmbeddings` — adds `Embedding vector(1536)`, `EmbeddingModel`, `EmbeddedAt` to `document_chunks`.
4. `20260709200821_AddConversations` — `conversations`, `conversation_messages`.
5. `20260718214608_AddExternalIdentityFields` — adds `AuthenticationProvider` (default `"Local"`), `ExternalSubject` (filtered unique index), `LastLoginAt` to `users`. **← current head migration.**

Snapshot: `ApplicationDbContextModelSnapshot.cs` (EF 9.0.6, 7 tables).

### 3.2 ChatDbContext migrations
1. `20260718221730_InitialChat` — `conversations` + `conversation_messages` (no FK to Core). Snapshot: `ChatDbContextModelSnapshot.cs` (2 tables). **Only one migration.**

### 3.3 How migrations are applied
- **Core API** `Program.cs` 50–58: `context.Database.MigrateAsync()` + seeder **only when `IsDevelopment()`**.
- **Chat API** `Program.cs` 97–102: `MigrateAsync()` **only when `IsDevelopment()`**.
- **Worker**: no migration call.
- **CI** (`ci.yml`): build + test only, no `dotnet ef`.
- **CD** (`deploy.yml`): `docker compose up`; relies on API startup migration. `.env.example` defaults `ASPNETCORE_ENVIRONMENT=Development`, so migrations do run on container start in the current EC2 setup.
- **`deploy/docker/init-db.sql`**: `CREATE EXTENSION IF NOT EXISTS vector;` on first Core DB init only.

> **Production caveat:** if `ASPNETCORE_ENVIRONMENT=Production`, **no** auto-migration occurs and there is **no** CI/CD migration step. Any integration plan that flips to Production must add an explicit migration step.

---

## 4. CQRS / MediatR

- **Convention:** vertical slices `{Feature}/{Commands|Queries}/{Operation}/` in `ContactCenterAI.Application`. Commands/queries are `record`s implementing `IRequest<TResponse>`; handlers are `{Name}CommandHandler`/`{Name}QueryHandler : IRequestHandler<,>`. Response DTOs are plain classes (`{ get; set; }`), collections returned as `IReadOnlyList<T>`.
- **Pipeline behaviors:** only `Common/Behaviors/ValidationBehavior.cs`. Registered in `Application/DependencyInjection.cs` 15–18 (`AddMediatR`, `AddValidatorsFromAssembly`, `AddAutoMapper`, `ValidationBehavior<,>` as `IPipelineBehavior<,>`). **No logging/performance behavior.** AutoMapper is registered but **no `Profile` classes exist** (handlers map manually).
- **FluentValidation validators (Core):** `LoginCommandValidator`, `UploadDocumentCommandValidator` (PDF-only, ≤10MB, company required for non-SuperAdmin), `AskQuestionCommandValidator`, `SearchDocumentsQueryValidator`.
- **Existing Core use cases:** Commands — `LoginCommand`, `UploadDocumentCommand`, `AskQuestionCommand`. Queries — `GetCurrentUserQuery`, `ListCompaniesQuery`, `ListUsersQuery`, `ListDocumentsQuery`, `GetDocumentByIdQuery`, `SearchDocumentsQuery`, `ListConversationsQuery`, `GetConversationByIdQuery`.
- **Chat.Application** repeats the pattern but **does not register `ValidationBehavior`** (validators registered but never auto-invoked). Handlers take an explicit `BearerToken` and call Core API over HTTP.

**Convention new subagents MUST follow:** new features add a `{Feature}` folder with `Commands`/`Queries` subfolders, `record` requests, dedicated `*Handler` classes, FluentValidation `*Validator` in the same folder, and DTOs under `{Feature}/DTOs/`.

---

## 5. REST Endpoints

All endpoints are **MVC controllers** (`[Route("api/[controller]")]`); no minimal APIs. Only `[Authorize]` / `[AllowAnonymous]` — **no named policies, no role attributes** today.

### 5.1 Core API (`ContactCenterAI.Api/Controllers`)
| Verb | Route | Auth | Request | Response |
|---|---|---|---|---|
| POST | `api/Auth/login` | AllowAnonymous (410 when Auth0) | `LoginRequest` | `LoginResponseDto` |
| GET | `api/Auth/me` | Authorize | — | `CurrentUserDto` |
| GET | `api/Companies` | Authorize | — | `IReadOnlyList<CompanyDto>` |
| GET | `api/Users` | Authorize | — | `IReadOnlyList<UserDto>` |
| POST | `api/Documents` | Authorize | `[FromForm] UploadDocumentRequest` | `DocumentDto` |
| GET | `api/Documents` | Authorize | — | `IReadOnlyList<DocumentDto>` |
| GET | `api/Documents/{id:guid}` | Authorize | — | `DocumentDto` |
| POST | `api/Documents/search` | Authorize | `SearchDocumentsRequest` | `IReadOnlyList<SemanticSearchResultDto>` |
| POST | `api/Chat/ask` | Authorize (410 when External) | `AskQuestionRequest` | `AskQuestionResponse` |
| GET | `api/Chat/conversations` | Authorize | — | `IReadOnlyList<ConversationDto>` |
| GET | `api/Chat/conversations/{id:guid}` | Authorize | — | `ConversationDetailDto` |
| GET | `/health` | — | — | health |

### 5.2 Chat API (`ContactCenterAI.Chat.Api/Program.cs`, inline controller `api/chat`)
`POST api/chat/ask`, `GET api/chat/conversations`, `GET api/chat/conversations/{id:guid}` (all `[Authorize]`), `GET /health`. No Swagger.

---

## 6. Frontend (`src/frontend/contact-center-web`, React 19 + Vite + MUI v6)

- **Routing:** `react-router-dom` v7, `BrowserRouter` + nested `<Routes>` in `src/app/router.tsx`. Routes: `/login` (GuestRoute), and under `ProtectedRoute` + `PrivateLayout`: `/dashboard`, `/companies`, `/users`, `/documents`, `/chat`; `*` → `/dashboard`. **Guards are auth-only; no role-based route guards.**
- **Pages:** `CompaniesPage` (read-only list), `UsersPage` (read-only list), `DocumentsPage` (list + upload), `ChatPage` (full RAG + history), `LoginPage`. **No Tickets page.**
- **API layer:** native `fetch` wrappers in `src/shared/api/client.ts` (`apiGet`, `apiPost`, `apiPostFormData` — **no `apiPut`/`apiDelete` yet**). Base URL `VITE_API_BASE_URL` (default `http://localhost:8080`). Auth token attached per-request via `TokenProvider` (Local localStorage or Auth0 `getAccessTokenSilently`). Modules: `authApi.ts` (login/me/companies/users), `documentsApi.ts`, `chatApi.ts` (separate client for `VITE_CHAT_SERVICE_MODE=External`).
- **Auth:** `@auth0/auth0-react` v2; dual mode via `VITE_AUTH_PROVIDER`. Roles/company come from backend `/api/auth/me` (`CurrentUser`: `userId`, `email`, `role`, `companyId`, `companyName`, `isActive`), **not** from JWT claims. Role gating is minimal (only `user?.role === 'SuperAdmin'` in DocumentsPage).
- **Design system:** MUI v6 + Emotion, theme in `src/app/theme.ts`. Layout `src/layouts/PrivateLayout.tsx` (AppBar + Drawer). `navItems` array (25–31) defines the sidebar; `DashboardPage.tsx` has module cards.
- **Frontend shared hotspots for new pages:** `src/app/router.tsx`, `src/layouts/PrivateLayout.tsx` (navItems), `src/features/dashboard/DashboardPage.tsx` (module cards), `src/shared/api/client.ts` (needs PUT/DELETE), `src/shared/api/authApi.ts`, `src/shared/types/*`.

---

## 7. Authentication & Authorization

- **Dual mode** (`Authentication:Provider` / `AUTH_PROVIDER`, default `Local`), resolved in `Identity/AuthenticationConfiguration.cs`.
- **Auth0 JWT:** `Authority = https://{Domain}/`, `Audience` (default `https://contactcenterai-api`), RSA256, JWKS auto-fetch, `MapInboundClaims = false` (raw `sub`/`email`). Config in `Infrastructure/DependencyInjection.cs` `AddApiAuthentication` (86–154) + `Auth0TokenValidation.cs`.
- **Local JWT:** symmetric HMAC-SHA256 (`Jwt:SecretKey`), embeds role/companyId claims — but **these claims are ignored** for business logic.
- **User resolution pipeline:** `UseAuthentication()` → `LocalUserResolutionMiddleware` (Core API only, `Program.cs` 71) → `LocalUserResolver.ResolveAsync()` → stores `LocalUserContext` in `HttpContext.Items` → `CurrentUserService` reads `UserId`, `Email`, `Role`, `CompanyId`. **Role and CompanyId always come from the local `users` table, never from JWT claims.**
- **Auth0 ↔ User mapping:** by `ExternalSubject == sub`, else email match (then sets `ExternalSubject` + `AuthenticationProvider = Auth0`). No auto-provisioning; user must pre-exist. `user_not_registered` / `user_inactive` errors otherwise.
- **CompanyId resolution:** `User.CompanyId` (nullable). SuperAdmin has `CompanyId = null`. Tenant checks are **manual in handlers** (e.g. `SearchDocumentsQueryHandler.ResolveCompanyIdAsync`: SuperAdmin may pass/omit CompanyId, others locked to their own).
- **Authorization:** `AddAuthorization()` with **no custom policies**. Effective model: any valid JWT passes `[Authorize]`; tenant/role enforcement is code-level.

> **Multitenancy anchor points for feature (3):** `LocalUserResolver`, `ICurrentUserService.CompanyId`, per-handler tenant checks, and (recommended) a new EF **global query filter** keyed off an `ICurrentUserService.CompanyId` provider. Must tolerate SuperAdmin null-company.

---

## 8. Core API Structure

`ContactCenterAI.Api` — host + MVC controllers (Auth, Companies, Users, Documents, Chat[embedded]). `Program.cs` flow: `AddApplication()` → `AddInfrastructure()` → `AddApiAuthentication()` → controllers, Swagger (dev), health check on `ApplicationDbContext`, CORS `"Frontend"`. Middleware order: global exception handler → dev migrate/seed → Serilog → Swagger(dev) → CORS → Authentication → `LocalUserResolutionMiddleware` → Authorization → controllers → `/health`. Responsibilities: identity/auth, company/user/document management, document upload + semantic search, and an embedded chat mode (disabled with 410 when `CHAT_SERVICE_MODE=External`).

---

## 9. Chat API Microservice

- **Projects:** `Chat.Api` (host + inline controller, port 8081), `Chat.Application` (MediatR + DTOs + `IChatDbContext`/`IUserProfileClient`/`IDocumentSearchClient`/`IChatCompletionService`), `Chat.Domain`, `Chat.Infrastructure` (auth, CoreApi HTTP clients, Gemini, `ChatDbContext`).
- **Auth:** same dual-mode JWT validation as Core; **no `LocalUserResolutionMiddleware`**. Each handler extracts the raw Bearer token and forwards it.
- **Talks to Core:** `UserProfileClient` → `GET /api/auth/me` (resolves user + CompanyId + role); `DocumentSearchClient` → `POST /api/documents/search` (sends `{query, topK}`, **no CompanyId** — Core scopes by the token's user). `CoreApi:BaseUrl` = `http://api:8080` in compose.
- **Talks to own DB:** `ChatDbContext` (`contactcenterai_chat`), conversations keyed by `(CompanyId, ExternalUserId)`.
- **RAG pipeline:** validate token → resolve profile → `UserProfileValidator.EnsureValidForChat` (**requires non-null CompanyId** → SuperAdmin blocked from Chat) → resolve/create conversation → Core semantic search → build context → Gemini `generateContent` (`gemini-2.5-flash`, single-turn, no history sent) → persist user+assistant messages → return `AskQuestionResponse`.
- **Core semantic search:** `SemanticSearchService` generates a Gemini query embedding (`gemini-embedding-001`, 1536-d, L2-normalized) then raw SQL pgvector cosine (`<=>`) filtered `WHERE d."CompanyId" = @companyId`.

---

## 10. Worker Service

- **`ContactCenterAI.Worker`** = `BackgroundService` polling every `DocumentProcessing:IntervalSeconds` (default **30s**), batch `BatchSize` (default 5). **No queue.**
- **Pipeline:** upload → `Document (Uploaded)` + PDF on shared volume → worker query `WHERE Status IN (Uploaded, PendingProcessing)` → `Processing` → `PdfTextExtractor` (PDFiumZ) → `DocumentChunkingService` (char sliding window, size 1000/overlap 150) → `GeminiEmbeddingService` (per-chunk embedding, `RETRIEVAL_DOCUMENT`) → save `DocumentChunk` rows → `Processed`. Errors → `Failed` (error truncated 2000 chars), **no auto-retry**. `PendingProcessing` is defined but never assigned today.
- **RabbitMQ insertion point (feature 5):** publish `DocumentUploadedEvent` after `UploadDocumentCommandHandler` success; Worker becomes a consumer keyed on `documentId`; keep polling as a reconciliation fallback. `PendingProcessing` becomes the natural "enqueued" state.

---

## 11. Docker Compose

Single `docker-compose.yml`, default bridge network, 6 services:
| Service | Image/Build | Port | Notes |
|---|---|---|---|
| `db` | `pgvector/pgvector:pg16` | 5432 | main DB, `init-db.sql`, pg_isready healthcheck |
| `api` | `Dockerfile.api` | 8080 | depends_on db healthy; volumes `documents_data`, `api_logs` |
| `chat-db` | `postgres:16-alpine` | 5433 | no pgvector |
| `chat-api` | `Dockerfile.chat-api` | 8081 | `CoreApi__BaseUrl=http://api:8080`; depends_on chat-db healthy + api |
| `worker` | `Dockerfile.worker` | — | shares `documents_data`; polling settings |
| `web` | `Dockerfile.web` | 5173→80 | nginx SPA; Vite vars baked at build time |

Named volumes: `postgres_data`, `chat_postgres_data`, `documents_data`, `api_logs`, `chat_api_logs`, `worker_logs`. **pgAdmin is NOT present/exposed** (keep it that way).

- **RabbitMQ slot (feature 5):** add `rabbitmq:3-management` service on default network; `api` (publisher) and `worker` (consumer) `depends_on` it; expose 5672 (+15672 dev only).
- **BFF slot (feature 6):** add `bff` service (e.g. `:8082`) that calls `http://api:8080` and `http://chat-api:8080`; `web` `depends_on` bff. Optionally route `/graphql` via reverse proxy.

---

## 12. Production Config & CI/CD

- **`deploy/docker/`:** `Dockerfile.api`, `Dockerfile.worker`, `Dockerfile.web` (Node 22 → nginx:1.27-alpine), `Dockerfile.chat-api`, `init-db.sql`, `nginx.conf` (SPA fallback only, no API proxy). **No Caddyfile in repo** — Caddy HTTPS lives on the EC2 host, not version-controlled here.
- **CI (`.github/workflows/ci.yml`):** on push/PR to main/master/develop — `dotnet restore/build/test` on `ContactCenterAI.sln` + frontend `npm ci && npm run build`; `docker-build` job builds `api`, `worker`, `web` images. **Does NOT build `chat-api`** (gap). No migration step.
- **CD (`.github/workflows/deploy.yml`):** on push to main — SSH to EC2, `git reset --hard origin/main`, validate `.env`, `docker compose build && up -d`, health-check `/health` on 8080/8081. `.env` is host-only (gitignored), not created by workflow.
- **Env:** `.env.example` (root) drives compose `${VAR:-default}` substitution; ASP.NET uses `Section__Key` double-underscore mapping; Vite vars are build-time.

---

## 13. Existing / Partial Functionality per Feature Area

| Feature | State today | Gap |
|---|---|---|
| **(2) Admin companies/users** | `Company`/`User` entities, `ListCompaniesQuery`/`ListUsersQuery`, `GET api/Companies` + `GET api/Users`, read-only `CompaniesPage`/`UsersPage`, sidebar nav | Create/Update/Deactivate commands + endpoints + CRUD UI; role gating |
| **(3) Multitenancy** | `CompanyId` on User/Document/Conversation; per-handler manual tenant checks; `ICurrentUserService.CompanyId` | No global EF query filter; no `ITenantEntity`; inconsistent enforcement; SuperAdmin null-company handling |
| **(4) Tickets** | **Nothing** | Entity, config, migration, DbSet, CQRS, endpoints, DTOs, frontend page/route/nav |
| **(5) RabbitMQ / messaging** | **Nothing** (30s polling) | `IEventPublisher`, RabbitMQ client, event contracts, Worker consumer, compose service |
| **(6) GraphQL BFF** | **Nothing** | New BFF project/service, schema, resolvers calling REST, compose + routing |

---

## 14. Shared Files / Conflict Matrix

Legend: **2**=admin, **3**=multitenancy, **4**=tickets, **5**=rabbitmq, **6**=bff. ✅ = will modify/depend.

| Shared file / area | 2 | 3 | 4 | 5 | 6 | Conflict severity |
|---|---|---|---|---|---|---|
| `Infrastructure/Persistence/ApplicationDbContext.cs` (DbSets, `OnModelCreating`) | ✅ | ✅ | ✅ |  |  | **CRITICAL** |
| `Infrastructure/Persistence/Migrations/**` (+ model snapshot) | ✅ | ✅ | ✅ |  |  | **CRITICAL** (snapshot serializes) |
| `Domain/Identity/User.cs`, `Domain/Tenancy/Company.cs` | ✅ | ✅ |  |  |  | High |
| New `Domain/Tickets/Ticket.cs` (+ config) |  | ✅(filter) | ✅ | ✅(event) |  | Medium |
| `Application/DependencyInjection.cs` (behaviors) |  | ✅ |  | ✅ |  | Medium |
| `Infrastructure/DependencyInjection.cs` (DI) | ✅ | ✅ | ✅ | ✅ |  | High |
| `Api/Program.cs` (middleware/DI) |  | ✅ |  | ✅ | ✅ | High |
| `Api/Controllers/*` (new controllers) | ✅ |  | ✅ |  |  | Low (new files) |
| `ICurrentUserService` / `CurrentUserService.cs` | ✅ | ✅ | ✅ |  |  | High |
| `UploadDocumentCommandHandler.cs` (publish event) |  |  |  | ✅ |  | Medium |
| `Worker.cs` / `Program.cs` (consumer) |  |  |  | ✅ |  | Low |
| `docker-compose.yml` |  | | | ✅ | ✅ | **High** (two services adding blocks) |
| `.env.example` |  |  |  | ✅ | ✅ | Medium |
| `ContactCenterAI.sln` |  |  |  |  | ✅ | Medium (new project) |
| `.github/workflows/ci.yml` & `deploy.yml` |  |  |  | ✅ | ✅ | Medium |
| Frontend `src/app/router.tsx` | ✅ |  | ✅ |  |  | High |
| Frontend `src/layouts/PrivateLayout.tsx` (navItems) | ✅ |  | ✅ |  |  | High |
| Frontend `src/features/dashboard/DashboardPage.tsx` (cards) | ✅ |  | ✅ |  |  | Medium |
| Frontend `src/shared/api/client.ts` (add PUT/DELETE) | ✅ | | ✅ | | | Medium |
| Frontend `src/shared/api/authApi.ts`, `src/shared/types/*` | ✅ |  |  |  |  | Medium |

**Top hotspots (in priority order):**
1. **`ApplicationDbContext` + EF migrations + model snapshot** (2,3,4) — the single biggest risk. The auto-generated `ApplicationDbContextModelSnapshot.cs` is a serialized full model; every migration edits it → guaranteed merge conflicts if generated in parallel.
2. **`docker-compose.yml`** (5,6) — two features append services/env.
3. **`Api/Program.cs` + `Infrastructure/DependencyInjection.cs`** (2,3,5,6) — DI/middleware registration.
4. **Frontend `router.tsx` + `PrivateLayout.tsx` + `DashboardPage.tsx`** (2,4).
5. **`ICurrentUserService`** (2,3,4) — multitenancy will likely extend it.

---

## 15. Proposed Contracts

### 15.1 DTOs & REST endpoints — Companies (feature 2)
Extend existing `CompanyDto` (`{ Id, Name, Status, CreatedAt }`). Add:
```
POST   api/Companies            CreateCompanyRequest { Name }                       -> CompanyDto        (SuperAdmin)
PUT    api/Companies/{id:guid}  UpdateCompanyRequest { Name, Status }               -> CompanyDto        (SuperAdmin)
GET    api/Companies/{id:guid}                                                      -> CompanyDto
```
Keep existing `GET api/Companies` unchanged (additive only).

### 15.2 DTOs & REST endpoints — Users (feature 2)
Extend existing `UserDto` (`{ Id, Email, Role, IsActive, CompanyId, CompanyName, CreatedAt }`). Add:
```
POST   api/Users               CreateUserRequest { Email, Role, CompanyId?, Password? } -> UserDto   (SuperAdmin / CompanyAdmin scoped)
PUT    api/Users/{id:guid}     UpdateUserRequest { Role, IsActive, CompanyId? }         -> UserDto
GET    api/Users/{id:guid}                                                              -> UserDto
```
Tenant rule: `CompanyAdmin` may only manage users within their own `CompanyId`; `SuperAdmin` unrestricted.

### 15.3 DTOs & REST endpoints — Tickets (feature 4)
Proposed `Ticket` entity (in Core Domain, `AuditableEntity`): `Id`, `CompanyId : Guid` (required tenant), `Subject`, `Description`, `Status : TicketStatus` (`Open=1, InProgress=2, Resolved=3, Closed=4`), `Priority : TicketPriority` (`Low=1, Medium=2, High=3, Urgent=4`), `CreatedByUserId : Guid`, `AssignedToUserId : Guid?`, optional `ConversationId : Guid?` (link to chat).
```
TicketDto { Id, CompanyId, Subject, Description, Status, Priority, CreatedByUserId, AssignedToUserId?, CreatedAt, UpdatedAt? }
GET    api/Tickets                          (tenant-scoped list)          -> IReadOnlyList<TicketDto>
GET    api/Tickets/{id:guid}                                              -> TicketDto
POST   api/Tickets      CreateTicketRequest { Subject, Description, Priority, AssignedToUserId? } -> TicketDto
PUT    api/Tickets/{id:guid}  UpdateTicketRequest { Status, Priority, AssignedToUserId? }         -> TicketDto
```
`CompanyId`/`CreatedByUserId` derived server-side from `ICurrentUserService` (never trusted from client).

### 15.4 Event contracts & `IEventPublisher` (feature 5)
Place abstraction in `Application/Common/Interfaces` (or a shared `Application.Common`), implementation in `Infrastructure/Messaging`:
```csharp
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
}

// Events (immutable records; carry CompanyId for tenant-aware consumers)
public record DocumentUploadedEvent(Guid DocumentId, Guid CompanyId, Guid UploadedByUserId, DateTime OccurredAtUtc);
public record TicketCreatedEvent(Guid TicketId, Guid CompanyId, Guid CreatedByUserId, string Subject, DateTime OccurredAtUtc);
```
- Publisher registered in Core `Infrastructure/DependencyInjection.cs`; a **no-op/in-memory** implementation is the default fallback so nothing breaks if RabbitMQ is absent.
- `UploadDocumentCommandHandler` publishes `DocumentUploadedEvent` after successful save (keeps 30s polling as reconciliation fallback → zero regression risk).
- Worker consumer processes by `DocumentId`; `TicketCreatedEvent` consumers optional.
- Exchange/queue naming suggestion: topic exchange `contactcenter.events`, routing keys `document.uploaded`, `ticket.created`.

### 15.5 GraphQL schema shape (feature 6, BFF)
BFF is a **thin read-facing aggregator** over existing REST endpoints (forwards the Bearer token; no direct DB access). Suggested schema:
```graphql
type Query {
  me: CurrentUser
  companies: [Company!]!
  company(id: ID!): Company
  users(companyId: ID): [User!]!
  documents(companyId: ID): [Document!]!
  tickets(companyId: ID, status: TicketStatus): [Ticket!]!
  ticket(id: ID!): Ticket
  conversations: [Conversation!]!
  conversation(id: ID!): ConversationDetail
}

type Company { id: ID!  name: String!  status: String!  createdAt: DateTime!
  users: [User!]!  documents: [Document!]!  tickets: [Ticket!]! }
type User { id: ID!  email: String!  role: String!  isActive: Boolean!  companyId: ID  companyName: String  company: Company }
type Ticket { id: ID!  subject: String!  status: TicketStatus!  priority: TicketPriority!  company: Company  createdBy: User  assignedTo: User }
type Document { id: ID!  originalFileName: String!  status: String!  createdAt: DateTime! }
```
Mutations optional in phase 1 (keep REST authoritative for writes). Recommended engine: **HotChocolate** (native .NET/ASP.NET Core 9). BFF calls `http://api:8080` / `http://chat-api:8080` and reuses the same Auth0/JWT validation.

---

## 16. Integration Order & Dependencies

**Recommended order: admin → multitenancy → tickets → rabbitmq → graphql bff → integration tests.** Justification (based on real dependencies found):

1. **Admin (2) first** — extends already-existing entities/endpoints/pages (lowest risk, no new tables beyond CRUD on existing ones), and establishes the create/update handler + CRUD-UI patterns the others reuse. Ships a migration only if it adds columns.
2. **Multitenancy (3) second** — introduces the global tenant filter + `ITenantEntity` and hardens `ICurrentUserService.CompanyId`. Doing it before tickets means Ticket is born tenant-safe. Depends on admin's user/company management being stable.
3. **Tickets (4) third** — new tenant entity; consumes multitenancy's filter and `CompanyId` resolution, and reuses admin's CRUD conventions. Must be integrated after (3) so the Ticket table + filter land together.
4. **RabbitMQ (5) fourth** — depends on Worker's existing document pipeline and on tickets' `TicketCreatedEvent`. Purely additive (events + consumer + compose service); polling stays as fallback so nothing breaks.
5. **GraphQL BFF (6) fifth** — depends on the finalized REST contracts of companies/users/tickets/chat. Building last means the schema targets stable endpoints.
6. **Integration tests last** — validate the composed system end to end.

---

## 17. Dependencies Between Feature Modules

- **(3) multitenancy → (2) admin:** needs stable User/Company management and `ICurrentUserService` shape.
- **(4) tickets → (3) multitenancy:** Ticket relies on `CompanyId` resolution + global tenant filter; (4) also → (2) for CRUD conventions and user assignment lookups.
- **(5) rabbitmq → (4) tickets** (`TicketCreatedEvent`) **and → Worker/document pipeline** (`DocumentUploadedEvent`, consumer).
- **(6) bff → ALL** REST contracts (2 companies/users, 4 tickets, existing documents/chat/auth). Read-only aggregation; no DB coupling.
- **Cross-cutting:** all of (2),(3),(4) depend on the **shared `ApplicationDbContext` + migration chain**; (5),(6) depend on **shared `docker-compose.yml`**.

---

## 18. Conflict Detection & Mitigation (pre-work)

### 18.1 Migrations (CRITICAL)
- **Risk:** admin (possible column adds), multitenancy (query filter — usually no schema change, but may add indexes), and tickets (new table) each run `dotnet ef migrations add` against the **same `ApplicationDbContext`**, all rewriting `ApplicationDbContextModelSnapshot.cs` → unmergeable conflicts.
- **Mitigation:**
  1. **Serialize migration generation** in integration order (admin → multitenancy → tickets). Each feature rebases onto the previous feature's merged migration before generating its own.
  2. Appoint a **single "migration owner"** (the coordinator/Subagent 1) who regenerates/reconciles the snapshot at each integration step; feature subagents deliver entity + configuration changes and a **description** of the intended migration rather than committing conflicting snapshots.
  3. Never hand-edit the snapshot in parallel branches; regenerate after each merge.
  4. Keep `ASPNETCORE_ENVIRONMENT=Development` for auto-migrate during integration (matches current EC2 behavior); if moving to Production, add an explicit migration step to CI/CD first.

### 18.2 docker-compose.yml
- **Risk:** rabbitmq (add `rabbitmq` service + `depends_on`) and bff (add `bff` service) edit the same file/volumes/networks.
- **Mitigation:** assign **exclusive, append-only regions**; each adds its service block at the end and its volumes to the `volumes:` map. Coordinator merges. Keep the default bridge network. **Do not add pgAdmin.** Do not introduce serverless.

### 18.3 Program.cs / DI registration
- **Risk:** multitenancy (query-filter provider/middleware), rabbitmq (`IEventPublisher` + hosted consumer), bff (its own Program) touch DI/middleware.
- **Mitigation:** each feature registers via its **own `AddXxx()` extension method** invoked from a single line in `Program.cs`/`DependencyInjection.cs`, minimizing line-level overlap. Multitenancy owns any changes to `ICurrentUserService`.

### 18.4 Frontend routing / api client
- **Risk:** admin and tickets both edit `router.tsx`, `PrivateLayout.tsx` (navItems), `DashboardPage.tsx`, and `client.ts` (PUT/DELETE).
- **Mitigation:** add `apiPut`/`apiDelete` to `client.ts` **once** (assign to admin, first in order); tickets consumes them. Coordinate additive edits to `navItems`/routes/cards (append entries, don't reorder). New pages/types/api modules are new files (no conflict).

### 18.5 Preservation checklist (must remain intact)
- ✅ Auth0 login (dual-mode; do not change `MapInboundClaims`, Authority/Audience defaults).
- ✅ Gemini RAG (embeddings `gemini-embedding-001` 1536-d + `gemini-2.5-flash`; pgvector cosine).
- ✅ All existing REST endpoints (additive changes only; keep `GET api/Companies`/`api/Users` list shapes).
- ✅ Document upload → Worker → chunks/embeddings pipeline (RabbitMQ must be additive with polling fallback).
- ✅ Chat history model (Core embedded + external Chat DB).
- ✅ Docker Compose stack + healthchecks; **no pgAdmin**, **no serverless**.
- ✅ PostgreSQL + pgvector compatibility; existing data compatibility (new tenant filter must not orphan SuperAdmin null-company access; new columns nullable or defaulted).
- ✅ CI/CD to EC2 via SSH + Caddy on host (add `chat-api`/`bff` image builds to CI to close the existing gap).

---

## Appendix — Key File Index
- Core DbContext: `src/backend/ContactCenterAI.Infrastructure/Persistence/ApplicationDbContext.cs`
- Migrations: `src/backend/ContactCenterAI.Infrastructure/Persistence/Migrations/`
- Chat DbContext: `src/backend/ContactCenterAI.Chat.Infrastructure/Persistence/ChatDbContext.cs`
- Identity/auth: `src/backend/ContactCenterAI.Infrastructure/Identity/`
- CQRS: `src/backend/ContactCenterAI.Application/` (+ `ContactCenterAI.Chat.Application/`)
- Controllers: `src/backend/ContactCenterAI.Api/Controllers/`
- Worker: `src/backend/ContactCenterAI.Worker/` + `Infrastructure/Documents/`
- Frontend: `src/frontend/contact-center-web/src/{app,features,shared,layouts}/`
- Infra: `docker-compose.yml`, `deploy/docker/`, `.github/workflows/{ci,deploy}.yml`, `.env.example`
- Solution: `src/backend/ContactCenterAI.sln` (14 projects)
