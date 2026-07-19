# ContactCenterAI

Plataforma SaaS de soporte inteligente para agentes de contact center mediante RAG (Retrieval-Augmented Generation) sobre documentos PDF.

## Funcionalidades implementadas

- Login con JWT
- Roles y usuarios (SuperAdmin, Agent)
- Empresas (multi-tenant)
- Dashboard
- Carga de documentos PDF
- Procesamiento de documentos en Worker
- Extracción de texto desde PDF (PDFiumZ)
- Generación de chunks de texto
- Embeddings con Gemini API
- Búsqueda semántica con PostgreSQL + pgvector
- Chat RAG backend con respuestas basadas en documentos
- Historial de conversaciones

## Stack técnico

| Capa | Tecnología |
|------|------------|
| Backend | ASP.NET Core 9, Clean Architecture, CQRS + MediatR |
| Persistencia | Entity Framework Core, PostgreSQL + pgvector |
| Frontend | React, Vite, TypeScript, Material UI |
| IA | Gemini API (embeddings y chat) |
| Infraestructura | Docker, Docker Compose, GitHub Actions |

## Arquitectura

```text
src/
  backend/
    ContactCenterAI.Domain/          Entidades y reglas de negocio
    ContactCenterAI.Application/     Casos de uso, CQRS, interfaces
    ContactCenterAI.Infrastructure/  EF Core, servicios externos, IA
    ContactCenterAI.Api/             API REST protegida con JWT
    ContactCenterAI.Worker/          Procesamiento de documentos en background
  frontend/
    contact-center-web/              SPA React
deploy/docker/                       Dockerfiles y scripts de base de datos
tests/                               Pruebas unitarias xUnit
```

| Componente | Responsabilidad |
|------------|-----------------|
| **Api** | Endpoints REST, autenticación, documentos, búsqueda semántica, chat RAG |
| **Worker** | Procesa PDFs pendientes: extrae texto, genera chunks y embeddings |
| **Web** | Interfaz de usuario (login, dashboard, documentos) |
| **Database** | PostgreSQL con extensión pgvector para vectores |
| **Domain** | Entidades: usuarios, empresas, documentos, chunks, conversaciones |
| **Application** | Commands, queries, validaciones, DTOs |
| **Infrastructure** | Implementaciones: EF Core, almacenamiento local, Gemini API |

## Flujo RAG

```text
PDF subido → Worker extrae texto → genera chunks → Gemini genera embeddings
    → almacena vectores en pgvector → búsqueda semántica recupera chunks relevantes
    → Gemini Chat genera respuesta con contexto → respuesta con fuentes al agente
```

## Requisitos

- .NET 9 SDK
- Node.js 22+
- Docker y Docker Compose
- Cuenta en [Google AI Studio](https://aistudio.google.com/) para obtener `GEMINI_API_KEY`

## Variables de entorno

Copiar el archivo de ejemplo y completar los valores locales:

```bash
cp .env.example .env
```

Ejemplo seguro (sin claves reales):

```env
# PostgreSQL
POSTGRES_DB=contactcenterai
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

# API
API_PORT=8080
ASPNETCORE_ENVIRONMENT=Development
JWT_ISSUER=ContactCenterAI
JWT_AUDIENCE=ContactCenterAI.Client
JWT_SECRET_KEY=DEV_ONLY_SECRET_KEY_CHANGE_IN_PRODUCTION_123456

# Frontend
WEB_PORT=5173
WEB_ORIGIN=http://localhost:5173
VITE_API_BASE_URL=http://localhost:8080

# Proveedor de IA (Gemini)
AI_PROVIDER=Gemini
GEMINI_API_KEY=
GEMINI_EMBEDDINGS_MODEL=gemini-embedding-001
GEMINI_CHAT_MODEL=gemini-2.5-flash
GEMINI_EMBEDDING_DIMENSIONS=1536

# Document processing (Worker)
DocumentProcessing__IntervalSeconds=30
DocumentProcessing__ChunkSize=1000
DocumentProcessing__ChunkOverlap=150
DocumentProcessing__BatchSize=5
```

El archivo `.env` no se versiona. Las claves reales deben configurarse en variables de entorno locales o en GitHub Secrets para CI/CD.

## Ejecución local con Docker

```bash
cp .env.example .env
# Editar .env y agregar GEMINI_API_KEY

docker compose up -d db api web worker
```

| Servicio | URL |
|----------|-----|
| API Swagger | http://localhost:8080/swagger |
| API Health | http://localhost:8080/health |
| Frontend | http://localhost:5173 |
| PostgreSQL | localhost:5432 |

### Credenciales de desarrollo

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| admin@contactcenterai.cl | Admin123* | SuperAdmin |
| agente@contactcenterai.cl | Agent123* | Agent |

## Ejecución local sin Docker

```bash
# Backend
dotnet restore src/backend/ContactCenterAI.sln
dotnet build src/backend/ContactCenterAI.sln
dotnet run --project src/backend/ContactCenterAI.Api

# Worker (terminal separada)
dotnet run --project src/backend/ContactCenterAI.Worker

# Frontend
cd src/frontend/contact-center-web
cp .env.example .env
npm install
npm run dev
```

## Pruebas

```bash
dotnet build src/backend/ContactCenterAI.sln
dotnet test src/backend/ContactCenterAI.sln

cd src/frontend/contact-center-web
npm run build
```

## Endpoints principales

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/auth/login` | Autenticación JWT |
| POST | `/api/documents` | Subir PDF |
| GET | `/api/documents` | Listar documentos |
| POST | `/api/documents/search` | Búsqueda semántica |
| POST | `/api/chat/ask` | Pregunta RAG con fuentes |
| GET | `/api/chat/conversations` | Historial de conversaciones |
| GET | `/api/chat/conversations/{id}` | Detalle de conversación |

## CI/CD

**Integración continua (CI):** GitHub Actions (`.github/workflows/ci.yml`) ejecuta en cada push/PR a `main`, `master` y `develop`:

- `dotnet restore`, `build` y `test` del backend
- `npm ci` y `build` del frontend
- Build de imágenes Docker (api, worker, web)

**Despliegue continuo (CD):** GitHub Actions (`.github/workflows/deploy.yml`) despliega hacia AWS EC2.

- Se activa automáticamente con **push a `main`**
- También puede ejecutarse **manualmente** (`workflow_dispatch`)
- Secrets requeridos en el repositorio (sin valores en Git):
  - `EC2_HOST`
  - `EC2_USER`
  - `EC2_SSH_KEY`
- El archivo `.env` de producción **permanece únicamente en la instancia EC2**; el workflow no lo crea ni lo sobrescribe
- En EC2 se espera el clon del repositorio en `${HOME}/contactcenter-ai` (variable `PROJECT_PATH` del workflow; ajustable según la máquina)

## Decisión de proveedor de IA

| Proveedor | Estado | Motivo |
|-----------|--------|--------|
| Azure OpenAI | Descartado | Suscripción estudiantil deshabilitada |
| AWS Bedrock | Descartado | Entorno VocLabs sin permisos IAM |
| **Gemini API** | **En uso** | Proveedor configurable para embeddings (`gemini-embedding-001`) y chat (`gemini-2.5-flash`) |

La configuración se realiza mediante variables de entorno. No se hardcodean claves en el código ni en archivos versionados.

## Seguridad

- El archivo `.env` está en `.gitignore` y no debe subirse al repositorio.
- `GEMINI_API_KEY` y `JWT_SECRET_KEY` deben configurarse como secretos en entorno local o GitHub Secrets.
- Los endpoints de la API requieren JWT excepto login.
- El filtrado por empresa (tenant) aplica en documentos, búsqueda y chat.

## Pendiente

- Índice vectorial HNSW para optimización de búsqueda semántica
