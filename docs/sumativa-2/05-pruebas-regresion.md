# Pruebas de regresión

**Fecha:** 2026-07-19  
**Contexto:** Tras las correcciones históricas `ba0de32` (errores de validación), `c4cc3fc` (Worker/Docker/auth DI) y `b5ffbea` (GraphQL/CORS/proxy), se reejecutó la suite automatizada completa de la solución y el build del frontend.

## Funcionalidades verificadas después de las correcciones

| Área | Qué se revalidó | Cómo |
|------|-----------------|------|
| API / validaciones | Compilación del API con middleware de excepciones vigente | Build Release + suite Api/Application |
| Autenticación y roles | Login Local/Auth0, rechazo inactivo, roles en comandos | Infrastructure.Tests + Application.Tests |
| Empresas y usuarios | CRUD/listados y aislamiento | Application.Tests |
| Documentos y mensajería | Persistencia + publish + consumers/retry | Application + Infrastructure Messaging |
| Tickets y escalamiento | Aislamiento y flujo de eventos | Application + Infrastructure |
| Chat RAG | Persistencia, aislamiento, degradación Core/Gemini | Chat.Tests + Api EmbeddedChatGate |
| GraphQL BFF | Auth, aislamiento, degradación Core/Chat, health | Bff.Tests |
| Frontend | Empaquetado de producción | `npm run build` |
| Contenedores locales | Servicios Up y health HTTP | `docker compose ps` + `/health` |

## Pruebas automatizadas relacionadas

| Suite | Resultado 2026-07-19 |
|-------|----------------------|
| Domain.Tests | 1/1 OK |
| Application.Tests | 55/55 OK |
| Infrastructure.Tests | 45/45 OK |
| Api.Tests | 3/3 OK |
| Chat.Tests | 15/15 OK |
| Bff.Tests | 12/12 OK |
| **Total** | **131/131 OK, 0 fallidas** |

Evidencia: TRX en `artifacts/test-results/*.trx` y `03-resultados-pruebas-automatizadas.md`.

Los tests de Bff (`Graphql_*`, aislamiento, health) actúan como regresión directa de DEF-02. Los tests de mensajería/tickets y el build Docker del Worker (imagen `aspnet`) respaldan la estabilidad posterior a DEF-03. La presencia del middleware de excepciones + suite Application valida el contrato posterior a DEF-01.

## Resultado

- Build Release: correcto (0 errores, 0 advertencias).
- Tests: 131 superadas.
- Frontend build: correcto.
- Health local Api/Chat/Bff: HTTP 200 Healthy.

## Conclusión

No se observó regresión en la suite automatizada ni en el build del frontend tras las correcciones documentadas. Los defectos de dependencia NuGet (DEF-04, DEF-05) siguen abiertos y no fueron “cerrados” por esta regresión: la suite pasa, pero el escaneo de paquetes sigue reportándolos.

Limitación: no hay suite E2E de navegador; la regresión de UI GraphQL se cubre parcialmente por tests Bff y por el build TypeScript del cliente.
