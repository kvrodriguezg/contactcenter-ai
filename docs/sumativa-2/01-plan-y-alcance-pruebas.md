# Plan y alcance de pruebas — ContactCenterAI (Sumativa 2)

**Fecha del plan:** 2026-07-19  
**Solución:** `src/backend/ContactCenterAI.sln`  
**Frontend:** `src/frontend/contact-center-web`

## Objetivo

Verificar, con evidencias reproducibles, que el MVP de ContactCenterAI mantiene:

- autenticación (JWT local y Auth0);
- autorización por roles;
- aislamiento multiempresa (`CompanyId`);
- gestión de empresas, usuarios, documentos, tickets y chat RAG;
- mensajería RabbitMQ, BFF GraphQL, health checks, frontend y empaquetado Docker.

No se pretende demostrar cobertura total del código, sino demostrar que las pruebas existentes y las mediciones locales respaldan la preparación del MVP.

## Alcance

### Incluido

| Área | Fuente de verificación |
|------|------------------------|
| Backend .NET (Domain, Application, Infrastructure, Api, Worker, Chat, Bff) | Build Release + `dotnet test` sobre la solución |
| Proyectos de prueba en `/tests` | Seis proyectos xUnit referenciados por la solución |
| Frontend React/Vite | `npm ci` + `npm run build` |
| CI | `.github/workflows/ci.yml` (restore, build, test, npm build, docker build) |
| CD | `.github/workflows/deploy.yml` (despliegue SSH a EC2; no medido desde este equipo) |
| Seguridad de dependencias | `dotnet list package --vulnerable`, `npm audit`, workflow CodeQL (configurado) |
| Contenedores locales | `docker compose config`, `docker ps`, `docker stats --no-stream`, health HTTP |

### Proyectos de prueba identificados

1. `tests/ContactCenterAI.Domain.Tests`
2. `tests/ContactCenterAI.Application.Tests`
3. `tests/ContactCenterAI.Infrastructure.Tests`
4. `tests/ContactCenterAI.Api.Tests`
5. `tests/ContactCenterAI.Chat.Tests`
6. `tests/ContactCenterAI.Bff.Tests`

Todos están incluidos en `ContactCenterAI.sln`.

## Funcionalidades consideradas

Comprobadas mediante código, tests automatizados y/o configuración existente:

- autenticación Local JWT y Auth0;
- roles SuperAdmin, CompanyAdmin, Agent;
- empresas (CRUD / listado / estado);
- usuarios (creación, actualización, listado, ExternalSubject/Auth0);
- aislamiento por `CompanyId` (usuarios, documentos, tickets, conversaciones, GraphQL);
- documentos (validación PDF, publicación de evento de carga);
- procesamiento documental e idempotencia (reglas Worker / consumers);
- Chat RAG como microservicio (persistencia, aislamiento, contrato Core, Gemini no configurado);
- conversaciones;
- tickets y escalamiento;
- RabbitMQ (routing keys, serialización, retry, publishers/consumers, NoOp);
- GraphQL BFF (auth, aislamiento, degradación si Core/Chat caen);
- health checks (`/health` en Api, Chat Api, Bff);
- frontend (compilación de producción);
- Docker Compose (servicios en ejecución local durante esta revisión).

## Tipos de prueba

| Tipo | Dónde se aplica |
|------|-----------------|
| Unitarias / de aplicación | Application, Domain (parcial), reglas de mensajería |
| Integración liviana (InMemory / TestServer) | Infrastructure (auth), Api (gates), Chat, Bff GraphQL |
| Build / empaquetado | `dotnet build` Release, `npm run build`, imágenes en CI |
| Humo / salud | `GET /health` contra contenedores locales |
| Seguridad (dependencias y controles) | NuGet vulnerable, npm audit, revisión de Auth/roles/secretos |
| Regresión | Re-ejecución de la suite tras correcciones históricas documentadas en Git |

No hay suite E2E automatizada con navegador en el repositorio. Las pruebas E2E Auth0+Gemini descritas en `docs/evidence/chat-microservice-test-evidence.md` son checklist manual.

## Entorno de ejecución (esta evidencia)

| Elemento | Valor observado |
|----------|-----------------|
| SO | Windows 10 (build 26100) |
| Backend | .NET 9, configuración Release |
| Frontend | Node/npm en `contact-center-web` |
| Contenedores | Stack `docker compose` local activo (api, chat-api, bff, worker, web, db, chat-db, rabbitmq) |
| Auth en `.env` local | Modo Auth0 (según configuración local; no se versiona `.env`) |
| AWS EC2 | No medido desde este equipo (requiere acceso al host de producción) |

## Criterios de aceptación (para esta sumativa)

1. `dotnet restore` y `dotnet build --configuration Release` de la solución terminan sin error.
2. `dotnet test` sobre la solución reporta **0 fallidas**.
3. `npm ci` y `npm run build` del frontend terminan sin error.
4. Los casos documentados en `02-casos-prueba-ejecutados.md` corresponden a tests o mediciones reales.
5. Los defectos listados tienen evidencia en commits, tests o auditorías ejecutadas.
6. No se afirman métricas de AWS ni “cero vulnerabilidades” sin escaneo correspondiente.

## Exclusiones justificadas

| Exclusión | Motivo |
|-----------|--------|
| Pruebas de carga / stress formales | No existe herramienta ni suite de performance en el repo |
| Medición de latencia/CPU en AWS | El workflow CD opera por SSH en EC2; desde este equipo solo se midió entorno local |
| E2E Auth0 + Gemini end-to-end | Depende de secretos no versionados y checklist manual |
| Cobertura de código (%) | No se ejecutó Coverlet/reportes de cobertura en esta sesión |
| Domain.Tests / Api.Tests placeholder | Existe un `UnitTest1` trivial en Domain (y un test trivial histórico en Api); no se inventan casos de dominio adicionales |
| Pen testing / DAST | Fuera del alcance académico de esta evidencia |

## Relación con CI

El workflow `CI` (`.github/workflows/ci.yml`) ejecuta restore, build Release, test Release, `npm ci`, `npm run build` y, en job separado, `docker build` de Api, Worker y Web. Las evidencias locales de esta carpeta replican la parte de build/test/frontend; el job Docker de CI no se re-ejecutó aquí porque el stack local ya estaba levantado.
