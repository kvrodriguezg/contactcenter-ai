# Sumativa 2 — Evidencias de calidad (ContactCenterAI)

Índice de documentos generados a partir de auditoría del repositorio y ejecuciones del **2026-07-19**.

## Documentos

1. [01 — Plan y alcance de pruebas](01-plan-y-alcance-pruebas.md)
2. [02 — Casos de prueba ejecutados](02-casos-prueba-ejecutados.md)
3. [03 — Resultados de pruebas automatizadas](03-resultados-pruebas-automatizadas.md)
4. [04 — Registro de defectos y correcciones](04-registro-defectos-y-correcciones.md)
5. [05 — Pruebas de regresión](05-pruebas-regresion.md)
6. [06 — Evaluación de rendimiento](06-evaluacion-rendimiento.md)
7. [07 — Revisión de seguridad](07-revision-seguridad.md)
8. [08 — Informe final de calidad](08-informe-final-calidad.md)

## Artefactos locales

Ruta: `artifacts/test-results/` (ignorada por `.gitignore`; existe en el disco de trabajo).

- TRX por proyecto: `ContactCenterAI.*.Tests.trx`
- `resultados-pruebas-nota.txt`, `build-resumen.txt`, `ejecucion-resumen.txt`
- `nuget-vulnerable-audit.txt`, `npm-ci-summary.txt`, `npm-build-summary.txt`
- `health-timings.txt`, `docker-stats.txt`, `docker-ps.txt`

## Cómo reproducir las pruebas

Desde la raíz del repositorio:

```bash
dotnet restore src/backend/ContactCenterAI.sln
dotnet build src/backend/ContactCenterAI.sln --configuration Release
dotnet test src/backend/ContactCenterAI.sln --configuration Release --logger "trx;LogFileName=resultados-pruebas.trx" --results-directory artifacts/test-results
```

Si varios proyectos sobrescriben el mismo TRX, generar uno por proyecto:

```bash
dotnet test tests/ContactCenterAI.Application.Tests/ContactCenterAI.Application.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=ContactCenterAI.Application.Tests.trx" --results-directory artifacts/test-results
```

(Repetir para Domain, Infrastructure, Api, Chat y Bff.)

Frontend:

```bash
cd src/frontend/contact-center-web
npm ci
npm run build
```

Auditoría de dependencias:

```bash
dotnet list src/backend/ContactCenterAI.sln package --vulnerable --include-transitive
npm audit --prefix src/frontend/contact-center-web
```

Health y contenedores (con el stack levantado):

```bash
docker compose config --quiet
docker compose ps
docker stats --no-stream
curl http://127.0.0.1:8080/health
curl http://127.0.0.1:8081/health
curl http://127.0.0.1:8082/health
```

## Resultado clave de esta corrida

**131 pruebas superadas, 0 fallidas, 0 omitidas.**
