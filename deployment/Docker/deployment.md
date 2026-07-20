# Despliegue Docker — ContactCenterAI

Guía operativa del paquete de entrega en `deployment/Docker/`.  
Los archivos de esta carpeta son **copias de entrega**. El compose canónico del proyecto sigue en la raíz: `docker-compose.yml`. Los Dockerfiles originales están en `deploy/docker/`.

## Contenido del paquete

| Archivo / carpeta | Descripción |
|-------------------|-------------|
| `docker-compose.yml` | Stack completo (desarrollo / demo local) |
| `docker-compose.prod.yml` | Misma topología con defaults orientados a producción |
| `Dockerfiles/` | Copias de `Dockerfile.api`, `Dockerfile.chat-api`, `Dockerfile.bff`, `Dockerfile.worker`, `Dockerfile.web`, `nginx.conf`, `init-db.sql` |
| `deployment.md` | Este documento |

## Prerrequisitos

- Docker Engine + Docker Compose v2
- Archivo `.env` en la **raíz del repositorio** (partir de `.env.example`)
- `GEMINI_API_KEY` configurada
- En producción: `AUTH_PROVIDER=Auth0` y variables Auth0

## Importante: directorio de trabajo

Los builds usan `context: .` (raíz del repo) y rutas `deployment/Docker/Dockerfiles/...`.  
**Ejecutar siempre desde la raíz del repositorio.**

```bash
cd /ruta/a/Proyecto_Final_IA
cp .env.example .env
# Completar GEMINI_API_KEY y Auth0 si aplica
```

---

## Cómo levantar el sistema

### Opción A — Compose canónico (recomendado en desarrollo)

```bash
docker compose up -d
docker compose ps
```

### Opción B — Paquete de entrega (desarrollo)

```bash
docker compose -f deployment/Docker/docker-compose.yml --env-file .env up -d
docker compose -f deployment/Docker/docker-compose.yml ps
```

### Opción C — Paquete de entrega (producción)

```bash
docker compose -f deployment/Docker/docker-compose.prod.yml --env-file .env up -d
```

### Verificación de salud

```bash
curl http://localhost:8080/health   # Core API
curl http://localhost:8081/health   # Chat API
curl http://localhost:8082/health   # GraphQL BFF
```

| Servicio | URL típica |
|----------|------------|
| Frontend | http://localhost:5173 |
| Core Swagger | http://localhost:8080/swagger |
| GraphQL | http://localhost:8082/graphql |
| RabbitMQ Management (solo loopback) | http://127.0.0.1:15672 |

---

## Cómo detenerlo

```bash
# Compose de la raíz
docker compose down

# Paquete de entrega
docker compose -f deployment/Docker/docker-compose.yml down
# o
docker compose -f deployment/Docker/docker-compose.prod.yml down
```

Para eliminar también volúmenes (borra datos de PostgreSQL, PDFs y logs):

```bash
docker compose down -v
```

---

## Cómo reiniciarlo

```bash
docker compose restart
# o un servicio concreto:
docker compose restart api worker chat-api bff web
```

Reinicio completo (recrear contenedores sin rebuild):

```bash
docker compose up -d --force-recreate
```

---

## Cómo actualizarlo

```bash
git pull origin main
docker compose build
docker compose up -d --remove-orphans
docker compose ps
```

En AWS EC2 el workflow `.github/workflows/deploy.yml` automatiza: `git fetch` → `build` → `up -d` → health checks.

Actualización de un solo servicio:

```bash
docker compose build api
docker compose up -d api
```

---

## Cómo revisar logs

```bash
# Todos los servicios
docker compose logs -f --tail=200

# Por servicio
docker compose logs -f api
docker compose logs -f chat-api
docker compose logs -f worker
docker compose logs -f bff
docker compose logs -f web
docker compose logs -f db
docker compose logs -f chat-db
docker compose logs -f rabbitmq
```

Logs de aplicación (volúmenes Serilog):

| Contenedor | Volumen |
|------------|---------|
| `contactcenterai-api` | `api_logs` → `/app/logs` |
| `contactcenterai-chat-api` | `chat_api_logs` → `/app/logs` |
| `contactcenterai-worker` | `worker_logs` → `/app/logs` |
| `contactcenterai-bff` | `bff_logs` → `/app/logs` |

---

## Topología de servicios

```text
web (:5173) ──► nginx (estático + proxy /graphql → bff)
api (:8080) ──► db (pgvector :5432)
chat-api (:8081) ──► chat-db (:5433) + HTTP a api
bff (:8082) ──► HTTP a api + chat-api
worker ──► db + RabbitMQ + volumen documents_data
rabbitmq (:5672, UI :15672 loopback)
```

HTTPS en borde: snippet Caddy en `deploy/caddy/Caddyfile.graphql-bff.snippet` (no incluido en este paquete Docker; se configura en el host).

---

## Troubleshooting rápido

| Síntoma | Acción |
|---------|--------|
| `db` unhealthy | `docker compose logs db`; verificar `POSTGRES_*` |
| Chat 503 | Core API caído o `CoreApi__BaseUrl` incorrecto |
| Documentos en Pending | Revisar logs de `worker` y `GEMINI_API_KEY` |
| Login 410 | `AUTH_PROVIDER=Auth0` — usar Auth0, no formulario local |
| Build OOM | Construir por servicio: `docker compose build api` luego `chat-api` luego `web` |
