# Registro de defectos y correcciones

Solo se incluyen hallazgos con evidencia en commits, tests, configuración o auditorías ejecutadas el 2026-07-19. No se inventan filas para “completar” la tabla.

| ID | Descripción | Módulo | Gravedad | Impacto | Causa | Corrección | Evidencia | Estado final |
|----|-------------|--------|----------|---------|-------|------------|-----------|--------------|
| DEF-01 | Errores de validación/FluentValidation y excepciones de negocio no se devolvían de forma uniforme al cliente API | Api | Media | El cliente recibía fallos poco claros o no tipados en validaciones, dificultando diagnóstico en UI y pruebas | Falta de middleware global de excepciones al inicio del API | Se agregó `GlobalExceptionHandlerMiddleware`, `ApiErrorResponse` y extensión `UseGlobalExceptionHandler` | Commit `ba0de32` — *Se corrige manejo de errores de validación* | Corregido |
| DEF-02 | Integración GraphQL BFF con el frontend: orígenes CORS incompletos respecto a `WEB_ORIGIN` y proxy/nginx necesarios para el BFF | Bff / Frontend / Deploy | Alta | Consultas GraphQL desde el origen web configurado podían fallar por CORS o ruta incorrecta | CORS leía solo `Cors:Origins` y no consolidaba `WEB_ORIGIN`; faltaba ajuste de proxy nginx/Caddy y cliente GraphQL | `ResolveCorsOrigins` en Bff; cambios en `nginx.conf`, `Dockerfile.web`, `vite.config`, `graphqlClient.ts`, compose | Commit `b5ffbea` — *Correccion Graph QL* | Corregido |
| DEF-03 | Imagen Docker del Worker usaba runtime insuficiente para el host ASP.NET del Worker | Worker / Docker | Alta | El contenedor Worker podía no iniciar o comportarse de forma inestable en despliegue | `Dockerfile.worker` partía de `mcr.microsoft.com/dotnet/runtime:9.0` | Cambio a `mcr.microsoft.com/dotnet/aspnet:9.0`; además se separó `AddApiAuthentication` en Infrastructure | Commit `c4cc3fc` — *Se estabiliza configuración del Worker* | Corregido |
| DEF-04 | Dependencia `AutoMapper` 14.0.0 reportada como vulnerable (High) | Application / transitivos | Alta | Riesgo de DoS por recursión profunda en mapeos (`GHSA-rvv3-g6hj-g44x`) | Versión fijada en 14.0.0 (comentario de licencia comercial en 15+) | Pendiente: upgrade controlado o mitigación; no se cambió versión en esta sumativa | `dotnet list ... --vulnerable` 2026-07-19; advisory GHSA-rvv3-g6hj-g44x | Abierto |
| DEF-05 | Dependencia transitiva `Microsoft.SemanticKernel.Core` 1.47.0 reportada como Critical | Infrastructure / Api / Worker | Crítica | Advisory de Arbitrary File Write asociado a `SessionsPythonPlugin` (`GHSA-2ww3-72rp-wpp4`) | Paquete transitivo vía Semantic Kernel 1.47.0 | Pendiente upgrade a Core ≥ 1.71.0 con regresión; en `src/backend` no aparece uso de `SessionsPythonPlugin`, pero el hallazgo de dependencia permanece | Misma auditoría NuGet; búsqueda de texto en código | Abierto |
| DEF-06 | README raíz desactualizado frente al sistema actual | Documentación | Baja | Quien lea solo el README puede asumir un alcance menor (sin Auth0, BFF, tickets, RabbitMQ, microservicio Chat) | Documentación no sincronizada con merges posteriores | Pendiente de actualización editorial (no es bug de runtime) | Comparación `README.md` vs código/workflows actuales | Abierto (documental) |
| DEF-07 | `Domain.Tests` solo contiene un assert trivial | Domain.Tests | Baja | Influye poco en la confianza funcional; aporta 1 al conteo de suite sin cubrir entidades | Scaffold inicial de pruebas | Pendiente ampliar pruebas de dominio reales | `tests/ContactCenterAI.Domain.Tests/UnitTest1.cs` | Abierto (calidad de pruebas) |

## Criterios de gravedad usados

| Nivel | Criterio aplicado aquí |
|-------|------------------------|
| Crítica | Bloqueo de seguridad reportado como Critical por herramienta, o fallo que impide operar el servicio en producción |
| Alta | Impide usar un módulo principal (BFF/Worker) o vulnerabilidad High de dependencia |
| Media | Degrada experiencia o contrato API sin tumbar el sistema |
| Baja | Documentación o calidad de pruebas sin impacto inmediato en runtime |

## Notas

- Hallazgos operativos de entorno (por ejemplo conflicto de `container_name` con otro worktree en auditorías previas) no se registran como defectos de producto.
- El comportamiento `POST /api/auth/login` → 410 con `AUTH_PROVIDER=Auth0` es diseño documentado, no defecto.
- No se listan “vulnerabilidades cero” ni defectos sin evidencia.
