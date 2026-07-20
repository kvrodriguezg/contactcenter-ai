# Actualización técnica respecto de la planificación de Semana 3

## 1. Propósito

El entregable original de Semana 3 describe la planificación inicial del proyecto ContactCenterAI (arquitectura prevista, stack y alcance funcional). Este archivo no reemplaza ese documento: solo registra los cambios técnicos que quedaron en el código, la infraestructura y la documentación del repositorio durante la implementación del MVP.

La columna de planificación se toma del diseño académico inicial. La de implementación se limita a lo comprobable en el repositorio a la fecha de esta actualización.

## 2. Resumen de cambios

| Aspecto | Planificación inicial | Implementación final | Motivo del cambio | Evidencia en el repositorio |
|---------|----------------------|----------------------|-------------------|------------------------------|
| Proveedor de IA | Azure OpenAI | Gemini API (embeddings y chat) | Disponibilidad y ajuste al contexto académico | `README.md`; `GeminiEmbeddingService`; `Ai__Provider` / `GEMINI_API_KEY` en `docker-compose.yml` |
| Infraestructura | Azure | AWS EC2 | Despliegue operativo con Docker Compose en instancia EC2 | `.github/workflows/deploy.yml`; `README.md` |
| Procesamiento asíncrono | Azure Functions / Serverless | Worker Service (`BackgroundService`) | Modelo más simple de operar y depurar en Compose | `ContactCenterAI.Worker`; `deploy/docker/Dockerfile.worker` |
| Mensajería | Azure Service Bus | RabbitMQ (feature flag `MESSAGING_ENABLED`) | Broker localizable en Compose; desacopla documentos y tickets | `docker-compose.yml` (servicio `rabbitmq`); `RabbitMQ.Client` en Infrastructure |
| Autenticación | Identity Service genérico | Auth0 (JWT RS256) + login JWT local solo en desarrollo | IDaaS externo con roles y tenancy en BD local | `docs/architecture/auth0-integration.md`; `docs/security/authentication-authorization.md` |
| Persistencia | Base relacional general | PostgreSQL Core + pgvector; PostgreSQL Chat | Separación Core/Chat y búsqueda vectorial | `docker-compose.yml` (`db`, `chat-db`); `deploy/docker/init-db.sql`; paquetes `Pgvector` |
| Arquitectura | Microservicios planificados | Core API, Chat API, GraphQL BFF, Worker, RabbitMQ, PostgreSQL Core y Chat | Descomposición por bounded context sin stack Azure | `docker-compose.yml`; `docs/architecture/` |
| Despliegue | Servicios cloud genéricos | Docker Compose + Caddy HTTPS (snippet) en AWS EC2 | Un solo mecanismo de despliegue verificable | `docker-compose.yml`; `deploy/caddy/Caddyfile.graphql-bff.snippet`; `deploy.yml` |
| CI/CD | Pipeline planificado | GitHub Actions (CI, CD a EC2, CodeQL) | Pipelines versionados en el repo | `.github/workflows/ci.yml`, `deploy.yml`, `codeql.yml` |
| Seguridad | SonarQube planificado | Auditoría NuGet/npm, CodeQL y controles AuthN/AuthZ | SonarQube no integrado; se usaron controles presentes en el repo | `docs/sumativa-2/07-revision-seguridad.md`; `codeql.yml`; `docs/security/` |

## 3. Arquitectura final implementada

El sistema desplegable con `docker compose` queda así:

- **Frontend:** React + Vite + TypeScript (`src/frontend/contact-center-web`).
- **Core API:** ASP.NET Core 9 (`ContactCenterAI.Api`), puerto host 8080; empresas, usuarios, documentos, tickets y búsqueda.
- **Chat API:** servicio independiente (`ContactCenterAI.Chat.Api`), puerto 8081; conversaciones RAG contra Core y Gemini.
- **GraphQL BFF:** HotChocolate (`ContactCenterAI.Bff`), puerto 8082; agrega Core y Chat.
- **Worker Service:** `BackgroundService` para PDF, chunks y embeddings; consumidores RabbitMQ cuando la mensajería está activa.
- **RabbitMQ:** broker en Compose (`rabbitmq:3-management`).
- **PostgreSQL Core:** imagen `pgvector/pgvector:pg16` (extensión `vector` en `init-db.sql`).
- **PostgreSQL Chat:** `postgres:16-alpine` en puerto host 5433.
- **pgvector:** embeddings `vector(1536)` en el contexto Core.
- **Auth0:** autenticación productiva/configurada; modo `Local` para desarrollo.
- **Gemini:** embeddings y generación de respuestas de chat.
- **Docker Compose:** orquestación local y la usada en EC2.
- **AWS EC2:** destino del workflow de despliegue por SSH.
- **Caddy HTTPS:** snippet de reverse proxy para `/graphql` y rutas sugeridas en borde.

Detalle operativo: `docs/architecture/microservices-deployment.md` y `docs/architecture/chat-microservice.md`.

## 4. Funcionalidades logradas

Solo se listan capacidades con respaldo en código, Compose, tests o documentación vigente:

| Funcionalidad | Evidencia breve |
|---------------|-----------------|
| Autenticación y roles (SuperAdmin, CompanyAdmin, Agent) | Auth0 / Local; controllers y tests de identity |
| Gestión de empresas | `CompaniesController` + tests Application |
| Gestión de usuarios | `UsersController` + tests Application |
| Aislamiento multiempresa (`CompanyId` desde perfil local) | `docs/security/authentication-authorization.md` |
| Carga y procesamiento de PDF | `DocumentsController`; Worker |
| Indexación vectorial | Worker + pgvector + Gemini embeddings |
| Chat RAG con fuentes | Chat API / DTOs con `Sources`; modo Embedded o External |
| Historial de conversaciones | persistencia en `chat-db` |
| Tickets y escalamiento | `TicketsController`; eventos/consumers RabbitMQ |
| RabbitMQ | Compose + Infrastructure Messaging |
| GraphQL | BFF + `docs/api/graphql-bff-examples.md` |
| Health checks | `/health` en Api, Chat API y BFF |
| CI/CD | workflows CI, deploy EC2 y CodeQL |
| Despliegue cloud | `deploy.yml` → AWS EC2 + Compose |

## 5. Cambios de alcance

Se sustituyeron, respecto del plan académico inicial, el proveedor de IA (Azure OpenAI → Gemini), la nube (Azure → AWS EC2), el cómputo asíncrono (Functions → Worker), la mensajería (Service Bus → RabbitMQ), el identity genérico (→ Auth0), SonarQube (→ auditorías NuGet/npm y CodeQL) y el despliegue genérico (→ Compose + Caddy).

Se mantuvo el propósito funcional: SaaS multiempresa de soporte con RAG sobre PDF, roles, documentos, chat con fuentes, tickets y entrega automatizable.

Las decisiones respondieron sobre todo a disponibilidad de servicios, compatibilidad con Docker Compose en un entorno académico y tiempo de implementación. El objetivo funcional del proyecto no cambió.

## 6. Calidad, pruebas y seguridad

### Proyectos de prueba

| Proyecto | Rol |
|----------|-----|
| `ContactCenterAI.Domain.Tests` | Suite mínima |
| `ContactCenterAI.Application.Tests` | Casos de aplicación |
| `ContactCenterAI.Infrastructure.Tests` | Messaging, identity, etc. |
| `ContactCenterAI.Api.Tests` | API |
| `ContactCenterAI.Chat.Tests` | Chat / microservicio |
| `ContactCenterAI.Bff.Tests` | GraphQL BFF |

### Resultados documentados (2026-07-19) y verificación posterior

| Actividad | Estado comprobable |
|-----------|-------------------|
| Build backend Release | Compilación correcta (0 errores / 0 advertencias en evidencia Sumativa 2) |
| Tests backend | **131** superadas, 0 fallidas, 0 omitidas (`docs/sumativa-2/03-resultados-pruebas-automatizadas.md`) |
| Build frontend | `npm ci` + `npm run build` exitosos (Vite) |
| GitHub Actions | `ci.yml` (restore/build/test/frontend + imágenes Api/Worker/Web); `deploy.yml` (EC2); `codeql.yml` (C# y JS/TS) |
| Auditoría NuGet | Hallazgos previos: AutoMapper 14.0.0 (High) y SemanticKernel.Core 1.47.0 (Critical). Corregidos a AutoMapper **15.1.1** y Semantic Kernel **1.71.0**. Re-auditoría: sin paquetes vulnerables reportados |
| Auditoría npm | `found 0 vulnerabilities` (evidencia Sumativa 2; reconfirmado en verificación local) |
| CodeQL | Workflow configurado; **sin resultados de ejecución afirmados** en la documentación de Sumativa 2 |
| Controles AuthN/AuthZ | Auth0/JWT, roles locales, `CompanyId` desde BD; ver `docs/security/` |

### Hallazgos y correcciones (extracto)

| ID | Tema | Estado |
|----|------|--------|
| DEF-01 | Manejo uniforme de errores API | Corregido |
| DEF-02 | GraphQL / CORS / proxy | Corregido |
| DEF-03 | Runtime Docker del Worker | Corregido |
| DEF-04 / DEF-05 | Vulnerabilidades NuGet | Corregidos (segunda auditoría sin hallazgos) |
| DEF-07 | `Domain.Tests` trivial | Abierto (calidad de pruebas) |

Fuente: `docs/sumativa-2/04-registro-defectos-y-correcciones.md` y `07-revision-seguridad.md`.

### Estado final (acotado)

El MVP **compila, prueba y se orquesta con Compose**. La auditoría NuGet y `npm audit` **no reportaron vulnerabilidades** en las ejecuciones verificadas. Eso no equivale a “cero alertas CodeQL” ni a una prueba de penetración: CodeQL está configurado, pero su resultado en GitHub no se documentó como ejecutado en Sumativa 2. El job CI de imágenes Docker construye Api, Worker y Web; Chat API y BFF se construyen vía `docker compose build` en despliegue.

## 7. Trazabilidad con la planificación original

| Objetivo original | Estado | Evidencia | Observación |
|-------------------|--------|-----------|-------------|
| Autenticación y autorización por roles | Cumplido con ajuste tecnológico | Auth0 + perfil local; tests identity | Identity genérico → Auth0 |
| Gestión multiempresa (empresas/usuarios) | Cumplido | Controllers y tests Application | Sin cambio de objetivo |
| Ingesta y procesamiento de PDF | Cumplido con ajuste tecnológico | Worker + almacenamiento en volumen | Functions → Worker |
| Indexación / búsqueda semántica | Cumplido con ajuste tecnológico | pgvector + Gemini embeddings | Azure OpenAI → Gemini |
| Chat asistido (RAG) con contexto documental | Cumplido | Chat API; fuentes en DTOs | Chat como servicio separado |
| Mensajería / procesamiento desacoplado | Cumplido con ajuste tecnológico | RabbitMQ + feature flag | Service Bus → RabbitMQ |
| Tickets / escalamiento | Cumplido | Tickets + consumers | Integrado con mensajería |
| API agregada / BFF | Cumplido | GraphQL BFF | Presente en Compose |
| Pipeline CI/CD | Cumplido con ajuste tecnológico | GitHub Actions | Imágenes Chat/BFF no en job `docker-build` de CI |
| Despliegue en nube | Cumplido con ajuste tecnológico | EC2 + Compose + Caddy snippet | Azure → AWS |
| Análisis estático tipo SonarQube | Parcial | CodeQL + auditorías de dependencias | SonarQube no implementado |
| Health / operabilidad básica | Cumplido | `/health` Api, Chat, Bff | Documentado en README y Sumativa 2 |

## 8. Conclusión

La implementación final conserva el propósito original del proyecto: un MVP de contact center multiempresa con RAG sobre PDF, autenticación, tickets y entrega automatizable. El stack concreto difiere del plan de Semana 3 (Gemini, Auth0, RabbitMQ, Worker, PostgreSQL/pgvector, Compose en AWS EC2), de modo que el resultado sea funcional, desplegable y verificable con el código y las evidencias del repositorio.

## 9. Referencias internas

- [README principal](../../../README.md)
- [Documentación / índice](../../README.md)
- [Arquitectura](../../architecture/)
- [Seguridad](../../security/)
- [Sumativa 2](../../sumativa-2/)
- [Evidencias](../../evidence/)
- [Workflows](../../../.github/workflows/)
- [Docker Compose](../../../docker-compose.yml)
- [Pruebas](../../../tests/)
)
