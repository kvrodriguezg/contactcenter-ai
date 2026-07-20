# Revisión de seguridad — Sumativa 2

**Fecha actualización remediación NuGet:** 2026-07-19  
**Proyecto:** ContactCenterAI

No se declara “cero vulnerabilidades” fuera de lo que confirma la herramienta en la fecha indicada.

---

## 1. Controles de aplicación (resumen)

| Tema | Estado verificable |
|------|-------------------|
| Auth0 / JWT | Auth0 (RS256) o Local (HS256) según `AUTH_PROVIDER` |
| Autorización por rol | SuperAdmin, CompanyAdmin, Agent en BD local |
| Aislamiento `CompanyId` | Desde perfil local (`ICurrentUserService`), no desde el cliente |
| HTTPS | Compose local en HTTP; Caddy snippet para borde HTTPS en despliegue |
| Secretos | `.env` en `.gitignore`; ejemplos sin secretos reales |
| PDF | Validación `application/pdf`, extensión `.pdf`, máx. 10 MB |
| Análisis estático | Workflow CodeQL configurado (`.github/workflows/codeql.yml`) |

Detalle AuthZ: `docs/security/authentication-authorization.md`.

---

## 2. Remediación de vulnerabilidades NuGet

### 2.1 Hallazgos previos (auditoría inicial)

| ID | Paquete | Versión anterior | Gravedad | Advisory |
|----|---------|------------------|----------|----------|
| V1 | AutoMapper | 14.0.0 | High | [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x) |
| V2 | Microsoft.SemanticKernel.Core (transitivo) | 1.47.0 | Critical | [GHSA-2ww3-72rp-wpp4](https://github.com/advisories/GHSA-2ww3-72rp-wpp4) |

**Declaración directa:**

- `ContactCenterAI.Application.csproj` → `AutoMapper`
- `ContactCenterAI.Infrastructure.csproj` → `Microsoft.SemanticKernel` (trae `Microsoft.SemanticKernel.Core` de forma transitiva)

### 2.2 Correcciones aplicadas

| Paquete | Versión anterior | Versión corregida | Notas |
|---------|------------------|-------------------|-------|
| AutoMapper | 14.0.0 | **15.1.1** | Ajuste DI: `AddAutoMapper(cfg => { }, assembly)` (API v15). Dependencias `Microsoft.Extensions.*` elevadas a **10.0.0** por requisito de AutoMapper 15. Licencia comercial: opcional vía `AUTOMAPPER_LICENSE_KEY` (sin clave solo registra avisos; no se subieron secretos). |
| Microsoft.SemanticKernel | 1.47.0 | **1.71.0** | Alineado al umbral del advisory; paquetes SK del mismo metapaquete. |

**Cambio de código mínimo:** `ContactCenterAI.Application/DependencyInjection.cs` (firma `AddAutoMapper`).

### 2.3 Verificación post-remediación (2026-07-19)

| Paso | Resultado |
|------|-----------|
| `dotnet restore src/backend/ContactCenterAI.sln` | Exit 0 |
| `dotnet build ... --configuration Release` | Compilación correcta, 0 errores, 0 advertencias |
| `dotnet test ... --configuration Release` | **131 superadas, 0 fallidas, 0 omitidas** |
| `dotnet list ... package --vulnerable --include-transitive` | **Ningún proyecto reportó paquetes vulnerables** en los orígenes NuGet usados |

Extracto segunda auditoría:

```text
El proyecto "ContactCenterAI.Application" especificado no tiene paquetes vulnerables en los orígenes actuales.
El proyecto "ContactCenterAI.Infrastructure" especificado no tiene paquetes vulnerables en los orígenes actuales.
(... resto de proyectos de la solución: mismo mensaje ...)
```

### 2.4 Frontend (`npm audit`)

Ejecución previa en la misma jornada académica: `found 0 vulnerabilities` (ver evidencia en `artifacts/test-results/` local si existe).

---

## 3. CodeQL

| Campo | Valor |
|-------|-------|
| Workflow | `.github/workflows/codeql.yml` |
| Resultado en esta sesión | Configurado; **no** se ejecutó el análisis en GitHub aquí |
| Afirmación | No se afirma “cero alertas CodeQL” sin corrida en CI |

---

## 4. Resumen ejecutivo

| Fuente | Estado tras remediación |
|--------|-------------------------|
| NuGet `--vulnerable --include-transitive` | Sin paquetes vulnerables reportados (2026-07-19) |
| AutoMapper / Semantic Kernel | Actualizados a 15.1.1 / 1.71.0 |
| CodeQL | Pendiente de primera ejecución en GitHub |
| Controles AuthN/AuthZ/tenancy | Implementados y cubiertos por tests |

**Afirmación limitada:** la segunda auditoría NuGet **no reportó vulnerabilidades** en la solución en esa fecha. Eso no sustituye CodeQL ni un pen test.
