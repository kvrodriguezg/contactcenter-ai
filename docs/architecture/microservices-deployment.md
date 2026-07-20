# Despliegue multi-servicio — ContactCenterAI

## Componentes

| Servicio | Imagen / build | Puerto host | Health |
|----------|----------------|-------------|--------|
| `db` | pgvector/pgvector:pg16 | 5432 | `pg_isready` |
| `chat-db` | postgres:16-alpine | 5433 | `pg_isready` |
| `api` | `Dockerfile.api` | 8080 | `/health` |
| `chat-api` | `Dockerfile.chat-api` | 8081 | `/health` |
| `bff` | `Dockerfile.bff` | 8082 | `/health` |
| `worker` | `Dockerfile.worker` | — | logs |
| `web` | `Dockerfile.web` | 5173 | HTTP |
| `rabbitmq` | rabbitmq:management | 5672 (AMQP), 15672 (UI local) | healthcheck compose |

HTTPS en borde: `deploy/caddy/Caddyfile.graphql-bff.snippet`.

## Variables clave

- Core: `ConnectionStrings__DefaultConnection`, `AUTH_PROVIDER`, `CHAT_SERVICE_MODE`, Gemini, JWT/Auth0, `MESSAGING_ENABLED`, RabbitMQ
- Chat: `ConnectionStrings__ChatDatabase`, `CoreApi__BaseUrl=http://api:8080`, `AUTH_PROVIDER`, Gemini chat
- BFF: URLs Core/Chat, CORS / `WEB_ORIGIN`
- Web: `VITE_API_BASE_URL`, `VITE_CHAT_SERVICE_MODE`, `VITE_CHAT_API_BASE_URL`, `VITE_GRAPHQL_URL`, Auth0

## Arranque local

```bash
docker compose up -d
docker compose ps
curl http://localhost:8080/health
curl http://localhost:8081/health
curl http://localhost:8082/health
```

Construir secuencialmente (`api` luego `chat-api` luego `web`) si el host tiene poca RAM.

## Independencia

- Detener `chat-api`: Core (login, empresas, documentos) sigue.
- Detener `api`: Chat responde 503 en `/ask` y consultas de perfil.
