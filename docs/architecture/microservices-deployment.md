# Despliegue multi-servicio — ContactCenterAI

## Componentes

| Servicio | Imagen / build | Puerto host | Health |
|----------|----------------|-------------|--------|
| `db` | pgvector/pgvector:pg16 | 5432 | `pg_isready` |
| `chat-db` | postgres:16-alpine | no público | `pg_isready` |
| `api` | `Dockerfile.api` | 8080 | `/health` |
| `chat-api` | `Dockerfile.chat-api` | 8081 | `/health` |
| `worker` | `Dockerfile.worker` | — | logs |
| `web` | `Dockerfile.web` | 5173 | HTTP |

## Variables clave

- Core: `ConnectionStrings__DefaultConnection`, `AUTH_PROVIDER`, `CHAT_SERVICE_MODE`, Gemini, JWT/Auth0
- Chat: `ConnectionStrings__ChatDatabase`, `CoreApi__BaseUrl=http://api:8080`, `AUTH_PROVIDER`, Gemini chat
- Web: `VITE_API_BASE_URL`, `VITE_CHAT_SERVICE_MODE`, `VITE_CHAT_API_BASE_URL`, Auth0

## Arranque local

```bash
docker compose build chat-api
docker compose up -d db chat-db api worker chat-api web
docker compose ps
curl http://localhost:8080/health
curl http://localhost:8081/health
```

Construir secuencialmente (`api` luego `chat-api` luego `web`) si el host tiene poca RAM.

## Independencia

- Detener `chat-api`: Core (login, empresas, documentos) sigue.
- Detener `api`: Chat responde 503 en `/ask` y consultas de perfil.
