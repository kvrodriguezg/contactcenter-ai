# ContactCenterAI

Plataforma SaaS multiempresa de soporte para agentes de contact center, con asistencia conversacional basada en **RAG** sobre documentos PDF.

Este documento es el **README de entrega final** del proyecto universitario. Refleja la implementación real del repositorio.

---

## Descripción

ContactCenterAI permite:

- Autenticar usuarios con **Auth0** (producción) o **JWT local** (desarrollo).
- Administrar **empresas**, **usuarios** y roles (`SuperAdmin`, `CompanyAdmin`, `Agent`).
- Cargar **PDF**, procesarlos de forma asíncrona y generar **embeddings**.
- Consultar la base de conocimiento mediante **Chat IA** (Gemini).
- Agregar datos con un **GraphQL BFF**.
- Gestionar **tickets** de escalamiento.
- Desplegar el stack con **Docker Compose** en **AWS EC2**, con HTTPS opcional vía **Caddy**.

---

## Arquitectura

```text
Frontend (React + Vite + MUI)
    ├── Core API (:8080)     → PostgreSQL Core + pgvector
    ├── Chat API (:8081)     → PostgreSQL Chat
    ├── GraphQL BFF (:8082)  → Core API + Chat API
    └── Worker               → Core DB + RabbitMQ + storage PDF
```

| Servicio | Responsabilidad |
|----------|-----------------|
| Core API | Auth, empresas, usuarios, documentos, search, tickets, chat embebido (opcional) |
| Chat API | Conversaciones RAG (modo External) |
| BFF | Consultas GraphQL agregadas (HotChocolate) |
| Worker | Extracción PDF, chunks, embeddings |
| Web | SPA + nginx (proxy `/graphql` → BFF) |
| RabbitMQ | Eventos de procesamiento/escalamiento (feature flag) |

Detalle: [docs/architecture/](../architecture/) · Manual técnico: [Manual_Tecnico.md](./Manual_Tecnico.md)

---

## Tecnologías

| Capa | Stack |
|------|-------|
| Backend | ASP.NET Core 9, Clean Architecture, CQRS + MediatR, EF Core, Serilog |
| Chat | ASP.NET Core 9 (bounded context propio) |
| BFF | HotChocolate GraphQL |
| Frontend | React 19, Vite 6, TypeScript, Material UI 6, Auth0 SPA SDK |
| Datos | PostgreSQL 16, pgvector (`vector(1536)`) |
| IA | Google Gemini API (embeddings + chat) |
| Mensajería | RabbitMQ 3 |
| Infra | Docker Compose, nginx, Caddy (snippet), GitHub Actions, AWS EC2 |

> **No forma parte de la solución vigente:** Azure OpenAI, Amazon Cognito, Bedrock (descartados / históricos).

---

## Requisitos

- .NET 9 SDK
- Node.js 22+
- Docker y Docker Compose v2
- Clave `GEMINI_API_KEY` ([Google AI Studio](https://aistudio.google.com/))
- (Producción) Tenant Auth0 + instancia AWS EC2 + (recomendado) Caddy

---

## Instalación

```bash
git clone <url-del-repositorio>
cd Proyecto_Final_IA
cp .env.example .env
# Completar GEMINI_API_KEY y, si aplica, Auth0
```

### Con Docker (recomendado)

```bash
docker compose up -d
docker compose ps
```

| Servicio | URL |
|----------|-----|
| Frontend | http://localhost:5173 |
| Core API / Swagger | http://localhost:8080/swagger |
| Core health | http://localhost:8080/health |
| Chat health | http://localhost:8081/health |
| GraphQL BFF | http://localhost:8082/graphql |
| RabbitMQ UI (local) | http://127.0.0.1:15672 |

### Scripts SQL de entrega

Ubicación: [`scripts/`](../../scripts/)

| Script | Uso |
|--------|-----|
| `create_extensions.sql` | Extensión `vector` (pgvector) |
| `install_database.sql` | Creación BD + tablas (referencia; EF migra en runtime) |
| `seed.sql` | Usuarios/empresa demo |

### Paquete Docker de entrega

Ver [`deployment/Docker/deployment.md`](../../deployment/Docker/deployment.md).

---

## Configuración

1. Copiar `.env.example` → `.env`.
2. Alinear backend y frontend:
   - `AUTH_PROVIDER` ↔ `VITE_AUTH_PROVIDER`
   - `CHAT_SERVICE_MODE` ↔ `VITE_CHAT_SERVICE_MODE`
3. Configurar Auth0 (SPA + API audience `https://contactcenterai-api`) si `AUTH_PROVIDER=Auth0`.
4. Crear usuarios locales antes del primer login Auth0 (mismo email).

Documentación Auth0: [docs/architecture/auth0-integration.md](../architecture/auth0-integration.md)

---

## Variables de entorno

Plantilla completa: [`.env.example`](../../.env.example)

| Grupo | Variables clave |
|-------|-----------------|
| PostgreSQL Core | `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` |
| PostgreSQL Chat | `CHAT_POSTGRES_DB`, `CHAT_POSTGRES_USER`, `CHAT_POSTGRES_PASSWORD` |
| Auth | `AUTH_PROVIDER`, `AUTH0_DOMAIN`, `AUTH0_AUDIENCE`, `JWT_*` |
| Frontend Auth | `VITE_AUTH_PROVIDER`, `VITE_AUTH0_*` |
| IA | `GEMINI_API_KEY`, `GEMINI_EMBEDDINGS_MODEL`, `GEMINI_CHAT_MODEL` |
| Chat | `CHAT_SERVICE_MODE`, `VITE_CHAT_SERVICE_MODE`, `VITE_CHAT_API_BASE_URL` |
| Mensajería | `MESSAGING_ENABLED`, `RABBITMQ_*` |
| Puertos | `API_PORT`, `CHAT_API_PORT`, `BFF_PORT`, `WEB_PORT` |

---

## Docker

Dockerfiles originales: `deploy/docker/`  
Copia de entrega: `deployment/Docker/Dockerfiles/`

```bash
# Desarrollo (raíz)
docker compose up -d
docker compose logs -f worker
docker compose down

# Paquete de entrega
docker compose -f deployment/Docker/docker-compose.yml --env-file .env up -d
docker compose -f deployment/Docker/docker-compose.prod.yml --env-file .env up -d
```

---

## GitHub Actions

| Workflow | Descripción |
|----------|-------------|
| `ci.yml` | Restore, build, test .NET; build frontend; build imágenes API/Worker/Web |
| `deploy.yml` | Despliegue SSH a AWS EC2 (`main` o manual) |
| `codeql.yml` | Análisis estático CodeQL (C# y JS/TS) |

Secrets de deploy: `EC2_HOST`, `EC2_USER`, `EC2_SSH_KEY`.

---

## Despliegue

### Local

Ver sección Instalación + [deployment.md](../../deployment/Docker/deployment.md).

### AWS EC2

1. Clonar el repo en el host (`~/contactcenter-ai` según workflow).
2. Mantener `.env` de producción en la instancia (no versionado).
3. Push a `main` o disparar el workflow CD.
4. Verificar:

```bash
curl http://127.0.0.1:8080/health
curl http://127.0.0.1:8081/health
```

### HTTPS (Caddy)

Snippet: `deploy/caddy/Caddyfile.graphql-bff.snippet`  
Enruta `/api`, `/chat-api`, `/graphql` y frontend detrás de HTTPS.

---

## Compilación y pruebas (sin Docker)

```bash
dotnet restore src/backend/ContactCenterAI.sln
dotnet build src/backend/ContactCenterAI.sln -c Release
dotnet test src/backend/ContactCenterAI.sln -c Release

cd src/frontend/contact-center-web
npm ci
npm run build
```

Evidencias de calidad: [docs/sumativa-2/](../sumativa-2/)

---

## Credenciales locales (solo desarrollo)

Válidas únicamente con `AUTH_PROVIDER=Local`:

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| admin@contactcenterai.cl | Admin123* | SuperAdmin |
| agente@contactcenterai.cl | Agent123* | Agent |

Con Auth0, `POST /api/auth/login` responde **410**.

---

## Capturas

| # | Pantalla | Archivo sugerido |
|---|----------|------------------|
| 1 | Login | `docs/assets/capturas/01-login.png` |
| 2 | Dashboard | `docs/assets/capturas/04-dashboard.png` |
| 3 | Empresas | `docs/assets/capturas/05-empresas.png` |
| 4 | Usuarios | `docs/assets/capturas/06-usuarios.png` |
| 5 | Documentos / upload | `docs/assets/capturas/07-documentos.png` |
| 6 | Chat IA + fuentes | `docs/assets/capturas/10-chat-ia.png` |
| 7 | Tickets | `docs/assets/capturas/12-tickets.png` |

> *(Lugares reservados — agregar capturas reales antes de la entrega impresa/PDF.)*

Manual de usuario con guía paso a paso: [Manual_Usuario.md](./Manual_Usuario.md)

---

## Documentación de entrega

| Documento | Ruta |
|-----------|------|
| Manual técnico | [Manual_Tecnico.md](./Manual_Tecnico.md) |
| Manual de usuario | [Manual_Usuario.md](./Manual_Usuario.md) |
| README final | Este archivo |
| Scripts SQL | [`scripts/`](../../scripts/) |
| Docker entrega | [`deployment/Docker/`](../../deployment/Docker/) |

---

## Licencia

Uso académico — Proyecto Final de Inteligencia Artificial / Ingeniería.  
Todos los derechos reservados al equipo autor, salvo disposición institucional en contrario.

---

## Autor

**Proyecto:** ContactCenterAI  
**Contexto:** Entrega final universitaria  
**Equipo / autor:** *(completar nombre(s) del grupo)*  

Repositorio y documentación técnica adicional en [`docs/`](../README.md).
