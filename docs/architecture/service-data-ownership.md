# Propiedad de datos por servicio — ContactCenterAI

Define qué base de datos (o sistema) es dueño de cada dato tras la migración a microservicios.  
**Regla absoluta:** ningún servicio accede a la base de datos de otro servicio.

---

## 1. documents_db (Core / Document API + Document Worker)

Motor: PostgreSQL 16 + extensión `vector` (pgvector).

| Tabla | Dueño de escritura | Lectura | Notas |
|-------|-------------------|---------|-------|
| `companies` | Document API | Document API | Tenancy. Chat solo conoce `CompanyId` por claim/parámetro |
| `users` | Document API | Document API | Perfil local (ver sección 4). Incluye `CompanyId`, `Role`, `IsActive`, email de negocio |
| `refresh_tokens` | Document API | Document API | Uso asociado al modo JWT local; en Auth0 el acceso es vía token del IdP |
| `documents` | Document API (metadata/upload); Worker (status/processed) | Document API, Worker | Worker actualiza `Status`, `ProcessedAt`, `ErrorMessage` |
| `document_chunks` | Document Worker | Document API (search), Worker | Embeddings `vector(1536)` (o dimensión configurada) |
| `__EFMigrationsHistory` | Document API / Worker tools | — | Historial de migraciones de este bounded context |

### Extensiones y objetos

- Extensión `vector`.
- Índices actuales: `CompanyId`, `Status`, `(DocumentId, ChunkIndex)`, email único en `users`, etc.
- Índice/operador de distancia coseno usado por búsqueda semántica (`<=>`).

### Almacenamiento de archivos (no es tabla)

- Volumen local compartido Document API ↔ Worker (`DocumentStorage__BasePath`), o S3 en etapas posteriores.
- Metadatos de ruta viven en `documents.StoragePath` (documents_db).

### Endpoints que mutan documents_db

- Document API: CRUD empresas/usuarios (según permisos), login local, upload documento, lectura/search.
- Worker: procesamiento PDF → chunks → embeddings.

Chat API **nunca** tiene connection string a `documents_db`.

---

## 2. chat_db (Chat / RAG API)

Motor: PostgreSQL 16 (**sin** requisito de pgvector en el objetivo).

| Tabla | Dueño de escritura | Lectura | Notas |
|-------|-------------------|---------|-------|
| `conversations` | Chat API | Chat API | Guarda `CompanyId` y `UserId` como **referencias lógicas** (GUID), sin FK a documents_db |
| `conversation_messages` | Chat API | Chat API | Incluye `SourcesJson` con `DocumentId`, chunk, preview, score |
| `__EFMigrationsHistory` | Chat API | — | Migraciones propias del Chat context |

### Relación actual en el monolito (a romper)

Hoy en Domain:

- `Conversation` → navegación EF a `Company` y `User` (FK reales en una sola BD).
- `AskQuestionCommandHandler` lee `DocumentChunks` vía `IApplicationDbContext` en el mismo proceso.

En el objetivo:

- Eliminar FKs cross-context.
- Sustituir navegaciones `Company` / `User` por solo `CompanyId` / `UserId`.
- Sustituir lectura de chunks por HTTP a Document API (`SearchSimilarChunks` + contenido completo en la respuesta).

### Qué NO debe existir en chat_db

- Tablas `documents`, `document_chunks`, `companies`, `users`.
- Extensión pgvector (innecesaria si no hay embeddings aquí).

---

## 3. Datos administrados por Amazon Cognito

| Dato | Ubicación Cognito | Uso |
|------|-------------------|-----|
| Identificador estable | `sub` (UUID Cognito) | Clave de federación hacia perfil local |
| Email | atributo estándar | Login + claim |
| Password / MFA | Cognito | Credenciales; **no** se duplica `PasswordHash` a largo plazo |
| Estado de verificación email | Cognito | Políticas de User Pool |
| Access / ID / Refresh tokens | Cognito | Emitidos por Cognito; APIs validan vía JWKS |
| App clients / scopes | Cognito | Document API, Chat API, frontend |

Cognito **no** es dueño de:

- Documentos, chunks, embeddings.
- Conversaciones y mensajes.
- Catálogo de empresas.
- Roles de negocio definitivos (hasta que se decida custom attributes/grupos; por defecto viven en perfil local).

---

## 4. Datos que permanecen como perfil local (`users` en documents_db)

Tras adoptar Cognito, la tabla `users` sigue siendo necesaria para el dominio multi-tenant:

| Campo | Motivo de permanencia local |
|-------|-----------------------------|
| `Id` | PK interna (puede igualarse a Cognito `sub` o mapearse vía columna `CognitoSub`) |
| `Email` | Copia de negocio / búsqueda admin (sincronizada) |
| `CompanyId` | Tenancy de aplicación |
| `Role` | `SuperAdmin` / `CompanyAdmin` / `Agent` |
| `IsActive` | Soft-disable sin borrar identidad Cognito |
| Auditoría (`CreatedAt`, etc.) | Trazabilidad de aplicación |

Campos a deprecar gradualmente:

| Campo | Estrategia |
|-------|------------|
| `PasswordHash` | Nullable o vacío cuando el usuario solo autentica con Cognito; login local feature-flagged |
| `refresh_tokens` | Dejar de emitir con JWT propio; tabla eliminable en etapa final |

**Prohibido** mover `CompanyId` / documentos / chat a Cognito como almacén primario.

---

## 5. Prohibición de acceso directo entre bases de datos

### Reglas

1. Document API y Worker: **solo** `documents_db`.
2. Chat API: **solo** `chat_db`.
3. Ningún `DbContext` compartido entre servicios en runtime.
4. Ninguna vista, FDW, dblink o réplica lógica como atajo entre contextos en el MVP académico.
5. Integración Documents ↔ Chat: **únicamente HTTP** (contrato de búsqueda semántica).
6. Integración identidad: **JWT** + lectura de perfil local en Document API (`/api/auth/me`).

### Violaciones típicas a evitar

| Anti-patrón | Por qué falla |
|-------------|---------------|
| Chat API con connection string a documents_db “solo lectura” | Acoplamiento de esquema; rompe ownership |
| JOIN SQL entre `conversations` y `documents` | Imposible/ilegal con BDs separadas |
| FK EF de `Conversation.UserId` → `users` en otra BD | No soportado; debe ser referencia lógica |
| Worker escribiendo mensajes de chat | Cruza bounded contexts |
| Frontend apuntando a dos orígenes de API | CORS/cookies/JWT complejos; usar Nginx |

### Criterio de aceptación de ownership

- Un cambio de esquema en `document_chunks` no requiere migración de `chat_db`.
- Chat puede desplegarse con `documents_db` en mantenimiento **solo** degradando RAG, no perdiendo historial.
- Borrar un documento actualiza sources históricas solo como dato desnormalizado en `SourcesJson` (sin integridad referencial cruzada).

---

## 6. Inventario actual (monolito) → destino

| Tabla actual (única BD) | Destino |
|-------------------------|---------|
| `companies` | documents_db |
| `users` | documents_db (perfil local) |
| `refresh_tokens` | documents_db (transitorio) |
| `documents` | documents_db |
| `document_chunks` | documents_db |
| `conversations` | chat_db |
| `conversation_messages` | chat_db |

Identidad Cognito: fuera de PostgreSQL.

---

## 7. Implicaciones para EF Core

Hoy: un `ApplicationDbContext` / `IApplicationDbContext` con **todos** los `DbSet`.

Objetivo:

| Contexto | Proyecto / servicio | DbSets |
|----------|---------------------|--------|
| `DocumentsDbContext` | Document API + Worker | Companies, Users, RefreshTokens, Documents, DocumentChunks |
| `ChatDbContext` | Chat API | Conversations, ConversationMessages |

Migraciones: historiales **separados**; no mover migraciones existentes en esta etapa documental (implementación posterior según plan).
)
