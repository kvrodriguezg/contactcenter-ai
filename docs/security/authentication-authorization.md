# Autenticación y autorización — ContactCenterAI

## Separación de responsabilidades

| Capa | Responsable | Datos |
|------|-------------|-------|
| Autenticación | Local JWT **o** Auth0 | Credenciales / tokens |
| Autorización | Base de datos local (`users`) | `Role`, `CompanyId`, `IsActive` |
| Tenancy | Base de datos local | `CompanyId` del perfil, nunca del frontend |

## Principios de seguridad

1. **No confiar en `CompanyId` del cliente.** Los handlers usan `ICurrentUserService.CompanyId` resuelto desde BD. Un SuperAdmin puede indicar empresa objetivo en el body; agentes no.
2. **Roles solo locales.** Claims de Auth0 no otorgan `SuperAdmin` / `CompanyAdmin` / `Agent`.
3. **Usuario debe existir y estar activo** en `users` tras validar el token.
4. **No registrar tokens** en logs (Serilog de request logging no debe enriquecerse con Authorization).
5. **Secretos fuera de git.** `.env` ignorado; `.env.example` solo placeholders.
6. **Audience e issuer estrictos** en modo Auth0 (RS256, un solo tenant/domain).
7. **Login local deshabilitado de forma controlada** en Auth0 (`410`), nunca error 500.
8. **HTTPS metadata** obligatorio fuera de Development para Auth0 JWKS.

## Modos

### Local

- Algoritmo: HS256 con `Jwt__SecretKey`
- Emisor/audiencia: `Jwt__Issuer` / `Jwt__Audience`
- Endpoint: `POST /api/auth/login`

### Auth0

- Algoritmo: RS256 (JWKS del domain)
- Emisor: `https://{AUTH0_DOMAIN}/`
- Audiencia: `AUTH0_AUDIENCE` (exacta)
- Endpoint login local: deshabilitado (`410 Gone`)

## Resolución de usuario

Orden en Auth0:

1. Claim `sub` → `users.ExternalSubject`
2. Fallback email → asociar `ExternalSubject` de forma controlada
3. Validar `IsActive`
4. Exponer `UserId`, `Role`, `CompanyId` vía `ICurrentUserService`

## Endpoints

| Endpoint | Local | Auth0 |
|----------|-------|-------|
| `POST /api/auth/login` | Activo | 410 Gone |
| `GET /api/auth/me` | Activo | Activo (Bearer Auth0) |
| APIs de negocio `[Authorize]` | JWT local | JWT Auth0 + perfil local |

## Amenazas mitigadas

| Amenaza | Mitigación |
|---------|------------|
| Token de otro tenant Auth0 | `ValidIssuer` = domain configurado |
| Token de otra API | `ValidAudience` exacta |
| Escalada de rol vía claim | Roles solo desde BD |
| Acceso cross-tenant | `CompanyId` desde perfil local |
| Usuario Auth0 sin alta local | 401 `user_not_registered` |
| Usuario deshabilitado | 401 `user_inactive` |
| Filtración de secretos | Placeholders en ejemplos; `.env` no trackeado |

## Checklist operativo

- [ ] `AUTH_PROVIDER` y `VITE_AUTH_PROVIDER` alineados
- [ ] Usuarios locales creados antes del primer login Auth0
- [ ] Callback/logout URLs correctas en Auth0
- [ ] Audience API idéntica en backend y frontend
- [ ] Probar rollback a Local en staging antes de producción
