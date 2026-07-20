# Informe final de calidad — ContactCenterAI (Sumativa 2)

**Fecha:** 2026-07-19  
**Rol:** evidencia de QA sobre el estado real del repositorio y ejecuciones locales.

## Qué se probó

- Solución backend `ContactCenterAI.sln` (restore, build Release, test Release).
- Seis proyectos en `/tests` (Domain, Application, Infrastructure, Api, Chat, Bff).
- Frontend `contact-center-web` (`npm ci`, `npm run build`).
- Controles de autenticación, roles, multiempresa, documentos, mensajería, tickets, chat RAG, GraphQL, health y Docker local.
- Auditoría de dependencias NuGet/npm y revisión de controles de seguridad documentados en código.

## Resultados reales

| Actividad | Resultado |
|-----------|-----------|
| Restore | Exit 0 |
| Build Release | Correcto, 0 errores, 0 advertencias, **15.33 s** |
| Tests | **131 superadas, 0 fallidas, 0 omitidas** |
| Frontend build | Exit 0 (vite ~5.67 s) |
| Health local Api/Chat/Bff | HTTP 200 Healthy (158 / 45 / 30 ms) |
| `docker compose config` | Exit 0; 8 contenedores Up |

Desglose de pruebas: Domain 1, Application 55, Infrastructure 45, Api 3, Chat 15, Bff 12.

## Defectos encontrados

Documentados en `04-registro-defectos-y-correcciones.md`:

- Históricos corregidos: manejo de errores API (DEF-01), GraphQL/CORS/proxy (DEF-02), imagen Worker (DEF-03).
- Abiertos: vulnerabilidades NuGet AutoMapper High y SemanticKernel.Core Critical (DEF-04, DEF-05); README desactualizado (DEF-06); Domain.Tests trivial (DEF-07).

## Defectos corregidos

DEF-01, DEF-02 y DEF-03 constan en el historial Git (`ba0de32`, `b5ffbea`, `c4cc3fc`) y la suite actual pasa sobre el código corregido.

## Regresión

Re-ejecución completa de la suite y build frontend sin fallos. Ver `05-pruebas-regresion.md`.

## Rendimiento

Solo mediciones locales (build, tests, health, `docker stats`). Sin datos de AWS. Ver `06-evaluacion-rendimiento.md`.

## Seguridad

Controles Auth0/JWT, roles y `CompanyId` respaldados por tests y docs. Secretos fuera de git. PDF validado por tipo/tamaño. `npm audit`: 0. NuGet: 2 vulnerabilidades. CodeQL configurado sin resultados aún. Ver `07-revision-seguridad.md`.

## Limitaciones

- No hay E2E automatizado de navegador ni pruebas de carga.
- Domain.Tests casi no aporta valor de negocio.
- Un solo `resultados-pruebas.trx` para toda la solución se sobrescribe; la evidencia usable son seis TRX.
- AWS no medido desde este equipo.
- CodeQL no corrido en GitHub en esta sesión.
- Carpeta `artifacts/` está en `.gitignore` (evidencias locales).

## Conclusión de preparación del MVP

El MVP **compila, prueba y levanta en local** con la suite automatizada en verde (131/131) y el frontend empaquetando correctamente. Las correcciones históricas relevantes no reaparecen como fallos de regresión en esta corrida.

La preparación para entrega académica es **aceptable con reservas explícitas**: dependencias NuGet con advisories High/Critical abiertas, documentación raíz desfasada, ausencia de E2E y de métricas productivas en AWS. Esas reservas no anulan el resultado de las pruebas ejecutadas; deben figurar en cualquier afirmación de “listo para producción”.
