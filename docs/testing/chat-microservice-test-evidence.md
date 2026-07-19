# Evidencia de pruebas — Chat microservice

## Automatizadas

Ejecutar:

```bash
dotnet test src/backend/ContactCenterAI.sln
```

Cobertura Chat (`ContactCenterAI.Chat.Tests`):

- ChatDbContext independiente / persistencia mensajes
- Aislamiento por `CompanyId`
- Usuario inactivo / sin empresa
- Propagación Bearer a `/api/auth/me`
- Contrato búsqueda documental
- Core no disponible → 503
- Gemini no configurado → error controlado
- Feature flags Embedded/External
- Gate Embedded en Core (`EmbeddedChatGateTests`)

## Manual / E2E (cuando Auth0 y Gemini estén configurados)

Variables:

```text
AUTH_PROVIDER=Auth0
VITE_AUTH_PROVIDER=Auth0
CHAT_SERVICE_MODE=External
VITE_CHAT_SERVICE_MODE=External
```

Checklist:

- [ ] Login Auth0
- [ ] `GET /api/auth/me` 200
- [ ] Documentos visibles y procesados
- [ ] Pregunta a Chat API → respuesta + fuentes
- [ ] Conversación en `chat-db`
- [ ] Historial visible
- [ ] Aislamiento por empresa
- [ ] Health Core y Chat OK

## Independencia

- [ ] Stop `chat-api` → Core OK
- [ ] Start `chat-api` → Chat OK
- [ ] Stop `api` → Chat 503
- [ ] Start `api` → recuperación

## Rollback

- [ ] `CHAT_SERVICE_MODE=Embedded` → chat embebido operativo

## Nota

La evidencia E2E autenticada con Auth0 real depende de secretos locales (no en git) y del entorno Docker en ejecución.

En la verificación de esta rama, `docker compose config` fue válido, pero el daemon Docker Desktop no estaba disponible (`dockerDesktopLinuxEngine` pipe ausente), por lo que build/up de contenedores, health E2E, independencia y rollback runtime quedan pendientes de re-ejecutar con Docker activo.
