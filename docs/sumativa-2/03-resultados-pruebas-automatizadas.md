# Resultados de pruebas automatizadas

**Fecha de ejecución:** 2026-07-19 (hora local America/Santiago, aproximadamente 20:54–21:03 -04:00)  
**Máquina:** entorno de desarrollo Windows del proyecto ContactCenterAI

## Comandos utilizados

```bash
dotnet restore src/backend/ContactCenterAI.sln
dotnet build src/backend/ContactCenterAI.sln --configuration Release
dotnet test src/backend/ContactCenterAI.sln --configuration Release --logger "trx;LogFileName=resultados-pruebas.trx"
```

Para conservar un TRX por ensamblado (el logger con el mismo `LogFileName` sobrescribe resultados cuando los proyectos corren en paralelo), se reejecutó:

```bash
dotnet test tests/ContactCenterAI.<Proyecto>.Tests/... --configuration Release --no-build --logger "trx;LogFileName=ContactCenterAI.<Proyecto>.Tests.trx" --results-directory artifacts/test-results
```

Frontend:

```bash
cd src/frontend/contact-center-web
npm ci
npm run build
```

## Restore

| Campo | Resultado |
|-------|-----------|
| Comando | `dotnet restore src/backend/ContactCenterAI.sln` |
| Exit code | 0 |
| Extracto | Se restauraron Bff y Bff.Tests; *14 de 16 proyectos están actualizados para la restauración* |

## Build Release

| Campo | Resultado |
|-------|-----------|
| Comando | `dotnet build src/backend/ContactCenterAI.sln --configuration Release` |
| Exit code | 0 |
| Advertencias | 0 |
| Errores | 0 |
| Tiempo reportado por MSBuild | **00:00:15.33** |
| Resumen guardado | `artifacts/test-results/build-resumen.txt` |

Extracto:

```text
Compilación correcta.
    0 Advertencia(s)
    0 Errores
Tiempo transcurrido 00:00:15.33
```

## Pruebas automatizadas (.NET)

### Totales reales (esta ejecución)

| Proyecto | Superadas | Fallidas | Omitidas | Total |
|----------|-----------|----------|----------|-------|
| ContactCenterAI.Domain.Tests | 1 | 0 | 0 | 1 |
| ContactCenterAI.Application.Tests | 55 | 0 | 0 | 55 |
| ContactCenterAI.Infrastructure.Tests | 45 | 0 | 0 | 45 |
| ContactCenterAI.Api.Tests | 3 | 0 | 0 | 3 |
| ContactCenterAI.Chat.Tests | 15 | 0 | 0 | 15 |
| ContactCenterAI.Bff.Tests | 12 | 0 | 0 | 12 |
| **Total** | **131** | **0** | **0** | **131** |

| Campo | Valor |
|-------|-------|
| Exit code suite | 0 |
| Duración suite única (`dotnet test` solución, `--no-build`) | ~7462 ms |
| Duración re-ejecución TRX por proyecto | ~23434 ms |

### Archivos TRX

| Archivo |
|---------|
| `artifacts/test-results/ContactCenterAI.Domain.Tests.trx` |
| `artifacts/test-results/ContactCenterAI.Application.Tests.trx` |
| `artifacts/test-results/ContactCenterAI.Infrastructure.Tests.trx` |
| `artifacts/test-results/ContactCenterAI.Api.Tests.trx` |
| `artifacts/test-results/ContactCenterAI.Chat.Tests.trx` |
| `artifacts/test-results/ContactCenterAI.Bff.Tests.trx` |
| `artifacts/test-results/resultados-pruebas-nota.txt` |

Nota: un único `resultados-pruebas.trx` para toda la solución no es fiable en esta plataforma porque VSTest sobrescribe el mismo nombre entre proyectos paralelos. La evidencia usable son los seis TRX anteriores.

## Frontend

| Paso | Resultado |
|------|-----------|
| `npm ci` | Exit 0; *added 244 packages, audited 245 packages*; `found 0 vulnerabilities` |
| `npm run build` | Exit 0; `vite v6.4.3`; *built in 5.67s*; tiempo total del paso ~12208 ms |
| Advertencia | Chunk JS > 500 kB (`index-*.js` ~803.52 kB / gzip ~243.61 kB) |

Evidencia: `artifacts/test-results/npm-ci-summary.txt`, `npm-build-summary.txt`.

## Relación con GitHub Actions

| Workflow | Relación con esta ejecución |
|----------|-----------------------------|
| `.github/workflows/ci.yml` | Mismos pasos conceptuales: restore → build Release → test Release → `npm ci` → `npm run build`; luego job `docker-build` de Api/Worker/Web |
| `.github/workflows/deploy.yml` | Despliega por SSH a EC2; **no** se ejecutó ni midió desde este equipo |
| `.github/workflows/codeql.yml` | Análisis estático configurado; **no** produjo resultados en esta sesión local |

## Afirmaciones que no se hacen

- No se afirma “124 pruebas”: el número medido aquí es **131**.
- No se afirma cobertura porcentual (no se generó reporte Coverlet en esta sesión).
- No se afirma éxito del job Docker de CI ni del deploy AWS en esta corrida.
