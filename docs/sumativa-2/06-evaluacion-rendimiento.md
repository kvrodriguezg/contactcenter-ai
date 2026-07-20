# Evaluación de rendimiento

**Fecha de mediciones:** 2026-07-19  
**Alcance:** solo métricas obtenidas en este equipo. **No se inventan umbrales SLA ni cifras de AWS.**

## 1. Tiempo de build

| Medición | Valor |
|----------|-------|
| Comando | `dotnet build src/backend/ContactCenterAI.sln --configuration Release` |
| Tiempo MSBuild | **00:00:15.33** |
| Resultado | Compilación correcta |

## 2. Tiempo total de tests

| Medición | Valor |
|----------|-------|
| `dotnet test` solución Release `--no-build` (suite única) | **~7462 ms** |
| Re-ejecución por proyecto con TRX | **~23434 ms** |
| Resultado funcional | 131/131 OK |

## 3. Tiempo de respuesta de endpoints health (local)

Medido con `Invoke-WebRequest` contra contenedores en ejecución:

| Endpoint | HTTP | Cuerpo | Latencia |
|----------|------|--------|----------|
| `http://127.0.0.1:8080/health` (Core Api) | 200 | Healthy | **158 ms** |
| `http://127.0.0.1:8081/health` (Chat Api) | 200 | Healthy | **45 ms** |
| `http://127.0.0.1:8082/health` (Bff) | 200 | Healthy | **30 ms** |

Archivo: `artifacts/test-results/health-timings.txt`.

Estas latencias incluyen arranque de la petición desde el host Windows hacia Docker; no son un benchmark formal ni p95 bajo carga.

## 4. Frontend build

| Medición | Valor |
|----------|-------|
| `npm run build` | vite *built in **5.67 s***; paso completo ~**12208 ms** |
| Tamaño bundle principal | ~803.52 kB (gzip ~243.61 kB) — advertencia Vite > 500 kB |

## 5. Consumo de recursos de contenedores

Comando: `docker stats --no-stream` (instantánea, no promedio).

| Contenedor | CPU % | Memoria | Mem % |
|------------|-------|---------|-------|
| contactcenterai-web | 0.00% | 10.96MiB / 7.445GiB | 0.14% |
| contactcenterai-bff | 0.01% | 59.25MiB / 7.445GiB | 0.78% |
| contactcenterai-chat-api | 0.02% | 91.21MiB / 7.445GiB | 1.20% |
| contactcenterai-worker | 0.00% | 73.38MiB / 7.445GiB | 0.96% |
| contactcenterai-api | 0.02% | 81.76MiB / 7.445GiB | 1.07% |
| contactcenterai-chat-db | 0.00% | 21.34MiB / 7.445GiB | 0.28% |
| contactcenterai-rabbitmq | 0.40% | 140.1MiB / 7.445GiB | 1.84% |
| contactcenterai-db | 0.01% | 25.33MiB / 7.445GiB | 0.33% |

Archivo: `artifacts/test-results/docker-stats.txt`.

Estado general (`docker compose ps`): todos los servicios **Up**; bff, chat-api, chat-db, rabbitmq y db reportan **healthy**. Api, worker y web estaban Up sin etiqueta healthy en esa salida (depende de si el compose define healthcheck para esos servicios).

## 6. AWS / producción

El workflow `.github/workflows/deploy.yml` despliega por SSH a EC2 (`secrets.EC2_*`) y hace health checks en el host remoto (`curl` a `/health` de Api y Chat).

**Desde este equipo no se midió** latencia, CPU ni memoria de la instancia AWS: no hay acceso interactivo al host de producción en esta sesión. Cualquier cifra de rendimiento en la nube quedaría inventada.

## 7. Observaciones y limitaciones

- No existe suite de carga (k6, JMeter, NBomber, etc.) en el repositorio.
- Health checks miden disponibilidad, no throughput de chat RAG ni de embeddings.
- `docker stats --no-stream` es una muestra puntual en reposo relativo (servicios Up ~4 h).
- El bundle frontend grande (>500 kB) es una observación de build, no una medición de TTFB en navegador.

## Conclusión de rendimiento

En local, build y tests completan en el orden de segundos/decenas de segundos, los health checks responden en menos de 200 ms en esta corrida, y el consumo de memoria de los contenedores se mantiene bajo respecto al límite del host (~7.4 GiB). No se emite veredicto de capacidad productiva en AWS por falta de medición remota.
