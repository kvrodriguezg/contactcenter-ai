# Revisión de seguridad — Sumativa 2

**Fecha:** 2026-07-19  
**Proyecto:** ContactCenterAI  
**Base:** código, configuración, `.gitignore`, documentación en `docs/security`, auditorías ejecutadas en esta sesión.

No se declara “cero vulnerabilidades” a nivel de solución: el escaneo NuGet reportó hallazgos y CodeQL aún no produjo resultados en GitHub.

---

## 1. Autenticación Auth0 / JWT

| Aspecto | Evidencia |
|---------|-----------|
| Modos | Local (HS256) o Auth0 (RS256/JWKS), según `AUTH_PROVIDER` / `Authentication:Provider` |
| Auth0 | Issuer/audience estrictos; login local responde 410 en modo Auth0 |
| Resolución de usuario | Claim `sub` → `ExternalSubject`; fallback email controlado; exige usuario activo en BD |
| Documentación | `docs/security/authentication-authorization.md`, `docs/architecture/auth0-integration.md` |
| Pruebas | `Infrastructure.Tests/Identity/AuthenticationTests.cs` |

## 2. Autorización por rol

| Aspecto | Evidencia |
|---------|-----------|
| Roles | SuperAdmin, CompanyAdmin, Agent almacenados en BD local (no se confían roles Auth0 para privilegios) |
| Controles | Handlers de Application rechazan operaciones fuera de rol (p. ej. Agent no crea usuarios; CompanyAdmin no crea SuperAdmin) |
| Pruebas | `CreateUserCommandTests`, `CompanyCommandsTests`, `TicketCommandTests`, `BffGraphQlTests` |

## 3. Aislamiento por CompanyId

| Aspecto | Evidencia |
|---------|-----------|
| Principio | `CompanyId` desde `ICurrentUserService` (perfil local), no desde el cliente |
| Pruebas | Auth (`CompanyId_comes_from_local_profile_not_claims`), usuarios, tickets, chat, GraphQL (documentos/conversaciones/tickets) |

## 4. HTTPS

| Ámbito | Estado verificable |
|--------|--------------------|
| Docker Compose local | Servicios exponen HTTP en puertos 8080/8081/8082/5173 (`ASPNETCORE_URLS: http://+:8080`) |
| Auth0 metadata | Documentado: HTTPS metadata obligatorio fuera de Development para JWKS |
| Producción AWS | El deploy CD no configura TLS dentro del workflow SSH; TLS dependería de reverse proxy/host en EC2 (no inspeccionado desde aquí) |

Conclusión: en el entorno local medido el tráfico API es HTTP. No se afirma HTTPS end-to-end en producción sin evidencia del proxy en EC2.

## 5. Manejo de secretos

| Control | Evidencia |
|---------|-----------|
| `.env` no versionado | Entradas en `.gitignore` (`.env`, variantes, `*.secrets`, `secrets.json`) |
| Plantillas | `.env.example` y `src/frontend/contact-center-web/.env.example` con placeholders |
| CD | Secretos GitHub (`EC2_HOST`, `EC2_USER`, `EC2_SSH_KEY`) referenciados en `deploy.yml` |
| Logs | Bff evita enriquecer logs con Authorization (tests de forwarding) |

En esta revisión **no** se pegaron ni subieron valores de `.env` ni tokens.

## 6. `.gitignore`

Ignora, entre otros: `bin/`, `obj/`, `node_modules/`, `dist/`, `.env*`, `storage/`, `TestResults/`, `artifacts/`, secretos y `.cursor/`.  
Nota: `artifacts/` está ignorado; las evidencias locales de esta sumativa viven ahí pero no se versionan automáticamente.

## 7. Variables de entorno

Configuración vía env/`IConfiguration` para JWT, Auth0, Gemini, RabbitMQ, puertos, CORS/`WEB_ORIGIN`, modos Chat (`CHAT_SERVICE_MODE`). Ejemplos en `.env.example` sin secretos reales.

## 8. Validación de archivos PDF

| Capa | Control |
|------|---------|
| Backend | `UploadDocumentCommandValidator`: `ContentType == application/pdf`, extensión `.pdf`, tamaño ≤ 10 MB |
| Frontend | `DocumentsPage.tsx`: accept PDF y mensajes de error |
| Procesamiento | `PdfTextExtractor` (PDFiumZ) falla de forma controlada si el PDF no abre |

No se ejecutó fuzzing de PDFs maliciosos en esta sesión.

## 9. Auditoría de dependencias

### Backend — `dotnet list ... --vulnerable --include-transitive`

Ejecutado 2026-07-19. Exit 0. **Hallazgos:**

| Paquete | Severidad | Advisory |
|---------|-----------|----------|
| AutoMapper 14.0.0 | High | https://github.com/advisories/GHSA-rvv3-g6hj-g44x |
| Microsoft.SemanticKernel.Core 1.47.0 (transitivo) | Critical | https://github.com/advisories/GHSA-2ww3-72rp-wpp4 |

Proyectos Chat.* y Bff.* no reportaron paquetes vulnerables en esa salida.  
Evidencia: `artifacts/test-results/nuget-vulnerable-audit.txt` y sección histórica del mismo día en esta carpeta.

### Frontend — `npm audit`

```text
found 0 vulnerabilities
```

(Exit 0 en la ejecución de esta sesión.)

## 10. Análisis estático disponible

| Herramienta | Estado |
|-------------|--------|
| CodeQL | Workflow `.github/workflows/codeql.yml` (csharp + javascript-typescript; push/PR/schedule/workflow_dispatch) |
| Resultados CodeQL | **No ejecutados** en esta sesión local; no hay SARIF ni “cero alertas” afirmable |
| CI clásico | `.github/workflows/ci.yml` no incluye escaneo SAST aparte de CodeQL |

## 11. Resumen

| Tema | Veredicto breve |
|------|-----------------|
| AuthN/AuthZ / tenancy | Controles implementados y cubiertos por tests |
| Secretos en git | `.env` ignorado; ejemplos sin secretos |
| PDF | Validación de tipo/tamaño presente |
| npm | 0 vulnerabilidades reportadas |
| NuGet | 2 paquetes vulnerables (High + Critical) — abiertos |
| CodeQL | Configurado, sin resultados aún |
| HTTPS local | HTTP en compose local |

**No se afirma cero vulnerabilidades del producto.**
