# Plan de migración a microservicios — ContactCenterAI

Migración controlada desde el monolito modular actual hacia la arquitectura definida en `microservices-target.md`.  
Este documento es **solo planificación**; no implica cambios de código en la etapa de auditoría.

---

## 0. Diagnóstico del monolito actual (baseline)

### 0.1 Capas y dependencias (Clean Architecture)

| Proyecto | Depende de | Observación |
|----------|------------|-------------|
| `ContactCenterAI.Domain` | — | Entidades Chat, Documents, Identity, Tenancy |
| `ContactCenterAI.Application` | Domain | CQRS MediatR; `IApplicationDbContext` mezcla **todos** los DbSets |
| `ContactCenterAI.Infrastructure` | Application, Domain | Un `ApplicationDbContext`, JWT, Gemini, PDF, pgvector |
| `ContactCenterAI.Api` | Application, Infrastructure | Controllers Auth, Users, Companies, Documents, Chat |
| `ContactCenterAI.Worker` | Application, Infrastructure | Procesa documentos; comparte el mismo DbContext |

Cumple Clean Architecture de dependencias de proyectos. El acoplamiento bloqueante está en **contexto de persistencia único** y en **handlers de Chat que leen DocumentChunks**.

### 0.2 Módulos Documents vs Chat

**Documents (Core):**

- Domain: `Company`, `User`, `RefreshToken`, `Document`, `DocumentChunk`, `Role`, statuses
- Application: `Auth`, `Users`, `Companies`, `Documents` (upload, list, get, search)
- Infrastructure: storage, PDF, chunking, embeddings, `SemanticSearchService`, JWT, password hasher
- Worker: `IDocumentProcessingService`
- API: `AuthController`, `UsersController`, `CompaniesController`, `DocumentsController`

**Chat:**

- Domain: `Conversation`, `ConversationMessage`, `MessageRole`
- Application: `AskQuestion`, `ListConversations`, `GetConversationById`
- API: `ChatController`
- Infra compartida: `IChatCompletionService` (Gemini Chat) — hoy registrado en el mismo DI que embeddings

### 0.3 Dependencias bloqueantes para separar servicios

| # | Bloqueo | Severidad | Detalle |
|---|---------|-----------|---------|
| B1 | `IApplicationDbContext` único | Alta | Chat y Documents comparten DbSets y `SaveChanges` |
| B2 | FK EF `Conversation` → `Company` / `User` | Alta | Impide `chat_db` sin romper migraciones actuales |
| B3 | `AskQuestionCommandHandler` lee `DocumentChunks` | Alta | Acceso directo a datos Documents desde Chat |
| B4 | `AskQuestion` usa `ISemanticSearchService` in-process | Alta | Debe volverse cliente HTTP |
| B5 | `AskQuestion` usa `Companies` para SuperAdmin default | Media | Resolver empresa vía claim o endpoint Documents |
| B6 | DI monolítico Gemini Chat + Embeddings | Media | Tras split: Chat API solo chat; Document/Worker solo embeddings |
| B7 | Frontend un solo `API_BASE_URL` a `:8080` | Media | Requiere gateway o proxy antes de multi-puerto |
| B8 | Compose una sola BD `contactcenterai` | Media | Hay que introducir `documents_db` + `chat_db` |
| B9 | JWT local claims = User.Id | Media | Cognito `sub` distinto; necesita mapeo de perfil |
| B10 | Domain `User` navega a `Conversations` y `UploadedDocuments` | Baja | Navegaciones bidireccionales a eliminar al partir Domain por servicio |
| B11 | Migraciones EF monolíticas | Alta | No mover aún; planificar split de historial en etapa de BDs |
| B12 | Tests actuales placeholder | Baja | Poca red de seguridad automatizada |

### 0.4 Acceso actual a ApplicationDbContext

Handlers/servicios que usan `IApplicationDbContext` / `ApplicationDbContext`:

- Auth: Login, GetCurrentUser
- Users: ListUsers
- Companies: ListCompanies
- Documents: Upload, List, GetById, Search (company resolve)
- Chat: AskQuestion, ListConversations, GetConversationById
- Infrastructure: `SemanticSearchService`, procesamiento de documentos, seeder, health check EF

Worker y API comparten el mismo connection string.

### 0.5 Relaciones Conversation / User / Company / DocumentChunk

```text
Company 1──* User
Company 1──* Document 1──* DocumentChunk
Company 1──* Conversation *──1 User
Conversation 1──* ConversationMessage
AskQuestion ──(in-process)──> SemanticSearch ──> document_chunks + documents
AskQuestion ──(in-process)──> DocumentChunks (contenido completo)
```

Tras migración: Conversation guarda solo GUIDs; DocumentChunk solo vía HTTP.

---

## 1. Principios de la migración

1. Etapas pequeñas, cada una desplegable y reversible.
2. Feature flags antes de cortes duros.
3. No eliminar login local hasta Cognito validado.
4. No mover entidades/migraciones en la etapa 0 (auditoría).
5. Mantener `develop`/`main` intactos; trabajo en `feature/microservices-cognito`.
6. Preferir la solución más simple defendible académicamente.

---

## 2. Etapas

### Etapa 0 — Documentación y baseline (actual)

**Trabajo:** este set de documentos + build/test verdes.  
**Estimación:** 0.5–1 día  
**Salida:** diagnóstico, ownership, target, Cognito plan, riesgos.  
**Rollback:** N/A (solo docs).

---

### Etapa 1 — Feature flags y contratos internos

**Objetivo:** preparar conmutación sin cambiar comportamiento por defecto.

| Ítem | Descripción |
|------|-------------|
| Flags auth | `Auth__Mode`, `Auth__EnableLocalLogin`, `VITE_AUTH_MODE` |
| Flag RAG | `Chat__SemanticSearch__Mode=InProcess\|Http` (default InProcess) |
| Flag gateway | `VITE_API_BASE_URL` apuntando a Nginx cuando exista |
| Contrato | Definir DTO HTTP de search (espejo de `SemanticSearchResultDto` + content) |
| Health | Mantener `/health` en API actual; documentar futuros endpoints |

**Estimación:** 2–3 días  
**Criterio done:** flags leídos; default = comportamiento actual; tests/build OK.  
**Rollback:** defaults a Local + InProcess.

---

### Etapa 2 — Cognito (paralelo seguro, sin cortar local)

Ver detalle en `cognito-integration-plan.md`.

| Ítem | Descripción |
|------|-------------|
| User Pool + App Client | Cuenta AWS del proyecto |
| Validación JWKS Hybrid | Document API (luego Chat API) |
| Columna / mapeo `CognitoSub` | Perfil local |
| Frontend modo cognito | Detrás de `VITE_AUTH_MODE` |
| Login local | Sigue activo |

**Estimación:** 5–8 días  
**Criterio done:** Hybrid funciona en staging; local intacto.  
**Rollback:** `Auth__Mode=Local`.

---

### Etapa 3 — Extracción Chat API (mismo DB temporalmente)

**Objetivo:** segundo host ASP.NET con módulos Chat; **aún** puede apuntar a la misma BD (paso intermedio).

| Ítem | Descripción |
|------|-------------|
| Proyecto `ContactCenterAI.Chat.Api` | Controllers chat + DI mínimo |
| Mover/copiar handlers Chat | Application slice Chat |
| Cliente HTTP search | Con flag Http; InProcess deprecado en Chat host |
| Gemini Chat solo en Chat API | Quitar registro de chat completion del Document host cuando se separe |
| Compose servicio `chat-api` | Puerto 8081 |
| Frontend / Nginx | Path `/api/chat` → chat-api |

**Estimación:** 5–7 días  
**Criterio done:** Chat en proceso separado; Documents en API original; E2E ask funciona.  
**Rollback:** apagar chat-api; restaurar ChatController en monolito; flag InProcess.

**Nota:** Separar proceso antes que separar BD reduce riesgo.

---

### Etapa 4 — Bases de datos separadas

Ver `service-data-ownership.md`.

| Ítem | Descripción |
|------|-------------|
| `documents_db` / `chat_db` | Dos instancias o dos databases en Postgres |
| `DocumentsDbContext` / `ChatDbContext` | Sin FKs cross-context |
| Migraciones nuevas por contexto | Estrategia: baseline snapshot + scripts de copia de tablas chat |
| Eliminar lectura directa DocumentChunks desde Chat | Solo HTTP |
| Quit navegaciones Domain cross | `Conversation` sin `Company`/`User` nav |
| Worker | Solo documents_db |

**Estimación:** 6–10 días  
**Criterio done:** Chat API sin connection string a documents; RAG vía HTTP; datos migrados.  
**Rollback:** apuntar ambos servicios a BD unificada (backup previo obligatorio).

---

### Etapa 5 — Nginx como API Gateway + frontend un origen

| Ítem | Descripción |
|------|-------------|
| Contenedor/nginx.conf | `/api/chat/` → chat-api; resto `/api/` → document-api; `/` → web |
| Health agregada | `/health` o `/health/document`, `/health/chat` |
| CORS | Simplificar (mismo origen) |
| Compose | Servicios independientes documentados |
| `VITE_API_BASE_URL` | Origen del gateway |

**Estimación:** 2–4 días  
**Criterio done:** navegador solo habla con Nginx; APIs no expuestas públicamente (ideal).  
**Rollback:** publicar de nuevo API :8080 directa; actualizar env frontend.

---

### Etapa 6 — Pruebas

| Tipo | Alcance |
|------|---------|
| Unit | Handlers Chat con mock HTTP Documents |
| Integration | Search HTTP contract |
| E2E manual | Login local, login Cognito (si activo), upload, worker, ask, list conversations |
| Chaos ligero | Parar document-api → ask falla controlado; chat list OK |
| Auth | Token inválido 401 en ambos APIs |
| Regresión | Roles SuperAdmin / CompanyAdmin / Agent |

**Estimación:** 3–5 días (solapable)  
**Rollback:** N/A (calidad).

---

### Etapa 7 — Despliegue AWS (EC2)

| Ítem | Descripción |
|------|-------------|
| Compose en EC2 | documents-db, chat-db, document-api, chat-api, worker, nginx, web |
| Cognito | Authority de producción |
| Secretos | Env / SSM; no git |
| Health checks | Monitoreo básico |
| CI | Build de nuevas imágenes |

**Estimación:** 3–5 días  
**Criterio done:** smoke E2E en URL pública; monolito anterior retenido como artefacto de rollback.  
**Rollback:** redeploy compose monolito + BD backup; DNS/puertos anteriores.

---

### Etapa 8 — Hardening y limpieza (post-estable)

- `Auth__Mode=Cognito` only (cuando se autorice).
- Eliminar path InProcess.
- Deprecar `PasswordHash` / `refresh_tokens` si aplica.
- Documentar runbooks.

**Estimación:** 2–3 días  
**Rollback:** reactivar Hybrid/Local.

---

## 3. Estimación consolidada

| Etapa | Estimación | Dependencias |
|-------|------------|--------------|
| 0 Documentación | 0.5–1 d | — |
| 1 Feature flags | 2–3 d | 0 |
| 2 Cognito Hybrid | 5–8 d | 1 |
| 3 Extracción Chat API | 5–7 d | 1 (2 recomendable en paralelo) |
| 4 BDs separadas | 6–10 d | 3 |
| 5 Gateway Nginx | 2–4 d | 3 (ideal tras 3; puede solapar inicio) |
| 6 Pruebas | 3–5 d | Continuo desde 3 |
| 7 Despliegue AWS | 3–5 d | 4 + 5 + Cognito usable |
| 8 Limpieza | 2–3 d | 7 estable |
| **Total** | **~29–46 días persona** | Calendario académico típico: 6–10 semanas |

Orden recomendado de implementación: **0 → 1 → 3 (con search HTTP) → 5 → 4 → 2 (si no empezó en paralelo) → 6 continuo → 7 → 8**.

Variante: iniciar **2 Cognito** en paralelo a **3** tras flags, porque no requiere split de BD.

---

## 4. Cambios necesarios en Docker Compose (diseño)

Estado actual: `db`, `api`, `worker`, `web` — una BD, un API.

Objetivo (servicios separados):

```text
documents-db
chat-db
document-api
chat-api
document-worker
nginx          # gateway + opcionalmente estáticos
web            # build estático, o fusionado en nginx
```

Capacidades Compose:

- `depends_on` + healthchecks por servicio.
- Profiles: `core`, `chat`, `full`.
- Volúmenes: `documents_data` solo para document-api + worker.
- Variables: dos connection strings; Cognito; Gemini keys solo donde correspondan.

---

## 5. Cambios necesarios en frontend (diseño)

| Ahora | Objetivo |
|-------|----------|
| `VITE_API_BASE_URL=http://localhost:8080` | URL del gateway (ej. `http://localhost` o dominio) |
| Paths `/api/chat`, `/api/documents`, … | Sin cambio de paths (Nginx enruta) |
| Login solo local | Condicional Cognito |
| Un `Authorization` header | Igual |

No hace falta que el frontend conozca dos backends.

---

## 6. Matriz de riesgos técnicos

| ID | Riesgo | Probabilidad | Impacto | Mitigación | Criterio de rollback |
|----|--------|--------------|---------|------------|----------------------|
| R1 | Romper FKs Conversation→User/Company al partir BD | Alta | Alto | Etapa intermedia sin FK; migrar datos con script; tests de integridad lógica | Restaurar backup BD única; un solo connection string |
| R2 | Regresión RAG (search HTTP incompleto vs in-process) | Alta | Alto | Contrato con content completo; comparar scores en staging; flag InProcess | `Chat__SemanticSearch__Mode=InProcess` o Chat en monolito |
| R3 | Drift de claims Cognito vs User.Id local | Media | Alto | Tabla mapeo `CognitoSub`; `/me` como fuente de verdad de rol/empresa | `Auth__Mode=Local` |
| R4 | Downtime por split de migraciones EF | Media | Alto | Backup; ventana; scripts idempotentes; no borrar historial monolito hasta validar | Restore dump pre-split |
| R5 | Timeouts Nginx en upload/ask | Media | Medio | `client_max_body_size`, timeouts 60–120s; probar PDFs grandes | Exponer API directa temporalmente |
| R6 | Gemini rate limits al duplicar clientes HTTP | Media | Medio | Reutilizar límites; retries backoff; no procesar batches enormes | Reducir BatchSize worker |
| R7 | Chat sin Document API (cascada 503) | Media | Medio | Health dependiente; mensaje UX claro; circuit breaker simple | N/A funcional; priorizar recovery Document |
| R8 | Desincronización perfil local vs Cognito (email/rol) | Media | Medio | Provisioning admin; job de sync opcional | Corregir fila `users`; Hybrid login |
| R9 | Secretos filtrados en Compose/CI | Baja | Crítico | `.env` ignorado; secrets en host/SSM; rotación | Rotar keys; invalidar App Client |
| R10 | CI no construye nuevas imágenes | Media | Medio | Extender `ci.yml` en etapa 3/5 | Revertir workflow; build local |
| R11 | Pérdida de navegaciones Domain complica código compartido | Baja | Bajo | Libraries compartidas mínimas (DTOs/contracts); no Domain gigante compartido | Mantener contratos versionados |
| R12 | Dual JWT validation bugs (Hybrid) | Media | Alto | Tests con tokens Local y Cognito; documentar orden de schemes | Desactivar Hybrid → un solo modo |
| R13 | Worker y API escriben Status con race | Baja | Medio | Ya existe patrón batch; mantener transacciones cortas | Reprocesar documentos fallidos |
| R14 | Frontend cache token viejo tras cutover Cognito | Media | Medio | Logout forzado; bump storage key | Clear site data / cambiar `TOKEN_KEY` |
| R15 | Estimación académica insuficiente | Media | Medio | Priorizar Etapas 1–3–5; Cognito Hybrid mínimo | Congelar en arquitectura documental + flags |

---

## 7. Checklist de no-hacer (guardrails)

- [ ] No modificar `develop` / `main` directamente.
- [ ] No commit/push en etapas de auditoría sin pedido explícito.
- [ ] No tocar `.env` ni secretos.
- [ ] No implementar Cognito antes de flags.
- [ ] No mover entidades/migraciones antes de Etapa 4.
- [ ] No eliminar login local.
- [ ] No desplegar AWS hasta Etapa 7.
- [ ] No acceso cross-database.

---

## 8. Definición de “migración exitosa”

1. Frontend consume un único origen.
2. Document API + Worker + `documents_db` autónomos.
3. Chat API + `chat_db` autónomos; RAG vía HTTP.
4. Cognito valida identidad (local disponible o desactivado conscientemente).
5. Health checks independientes verdes.
6. Compose levanta componentes por separado.
7. Build y tests de la solución en verde.
8. Runbook de rollback probado al menos una vez en staging.
)
