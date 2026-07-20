# Casos de prueba ejecutados

**Fuente:** métodos `[Fact]` / `[Theory]` en `/tests` y mediciones locales del 2026-07-19.  
**Resultado global de la suite:** 131 superadas, 0 fallidas, 0 omitidas (ver `03-resultados-pruebas-automatizadas.md`).

Se listan casos representativos por módulo real. No se inventan escenarios sin respaldo en código o medición.

| ID | Módulo | Escenario | Precondición | Pasos resumidos | Resultado esperado | Resultado obtenido | Estado | Tipo | Evidencia técnica |
|----|--------|-----------|--------------|-----------------|--------------------|--------------------|--------|------|-------------------|
| CP-01 | Autenticación | Acceso sin token | API/auth test host | Solicitar recurso protegido sin Authorization | Fallo de autenticación | Rechazo observado en test | OK | Integración | `AuthenticationTests.Access_without_token_fails` → Infrastructure.Tests |
| CP-02 | Autenticación | Login local resuelve por claim user id | Proveedor Local + usuario en BD InMemory | Login y consulta de identidad | Usuario autenticado con perfil local | Superado | OK | Integración | `Local_login_resolves_by_user_id_claim` |
| CP-03 | Autenticación | Auth0 asocia por `sub` / ExternalSubject | Proveedor Auth0 simulado | Token con `sub` mapeado a usuario | Usuario resuelto | Superado | OK | Integración | `Auth0_associates_by_external_subject`, `Auth0_resolves_authenticated_user_by_sub_claim` |
| CP-04 | Autenticación | Usuario inactivo rechazado | Usuario `IsActive=false` | Autenticar | Rechazo | Superado | OK | Integración | `Inactive_user_is_rejected` |
| CP-05 | Autenticación | Feature flag Local vs Auth0 | Configuración de proveedor | Resolver modo | Local habilita login; Auth0 lo deshabilita | Superado | OK | Unitaria | `Feature_flag_resolves_provider`, `Auth0_provider_signals_local_login_disabled` |
| CP-06 | Roles / usuarios | Agent no puede crear usuarios | Actor Agent | `CreateUserCommand` | Rechazo por autorización | Superado | OK | Aplicación | `CreateUserCommandTests.Agent_actor_cannot_create_users` |
| CP-07 | Roles / usuarios | CompanyAdmin no crea SuperAdmin | Actor CompanyAdmin | Crear usuario rol SuperAdmin | Rechazo | Superado | OK | Aplicación | `CompanyAdmin_cannot_create_super_admin` |
| CP-08 | Roles / usuarios | SuperAdmin crea agente local | Actor SuperAdmin, empresa activa | CreateUser con password | Usuario con hash | Superado | OK | Aplicación | `SuperAdmin_creates_local_agent_with_hashed_password` |
| CP-09 | Usuarios / Auth0 | ExternalSubject obligatorio en modo Auth0 | `IAuthProviderMode` Auth0 | CreateUser sin ExternalSubject | Validación falla | Superado | OK | Aplicación | `Auth0_mode_rejects_missing_external_subject` |
| CP-10 | Usuarios | ExternalSubject duplicado | Ya existe otro usuario con mismo `sub` | CreateUser | Rechazo | Superado | OK | Aplicación | `Duplicate_external_subject_is_rejected` |
| CP-11 | Empresas | Solo SuperAdmin lista todas | Varias empresas | `ListCompaniesQuery` | SuperAdmin ve todas; CompanyAdmin solo la propia | Superado | OK | Aplicación | `ListCompaniesQueryTests` |
| CP-12 | Empresas | CreateCompany sin SuperAdmin | Actor no SuperAdmin | CreateCompany | Rechazo | Superado | OK | Aplicación | `CreateCompany_without_super_admin_is_rejected` |
| CP-13 | Aislamiento multiempresa | CompanyId desde perfil, no desde claims | Token con claim de empresa distinta | Resolver usuario | CompanyId del perfil local | Superado | OK | Integración | `CompanyId_comes_from_local_profile_not_claims` |
| CP-14 | Aislamiento multiempresa | CompanyAdmin no lee usuario de otra empresa | Dos empresas | `GetUserByIdQuery` | Rechazo / no acceso | Superado | OK | Aplicación | `CompanyAdmin_cannot_read_user_from_other_company` |
| CP-15 | Documentos | Publica DocumentUploadedEvent al persistir | Messaging habilitado (fake) | UploadDocument | Evento publicado; documento persistido | Superado | OK | Aplicación | `UploadDocumentPublishTests.Publishes_DocumentUploadedEvent_after_persist_when_messaging_enabled` |
| CP-16 | Documentos | Fallo de publish no pierde documento | Publisher falla | UploadDocument | Documento permanece persistido | Superado | OK | Aplicación | `Publish_failure_does_not_lose_persisted_document` |
| CP-17 | Documentos (validación) | Solo PDF y tamaño ≤ 10 MB | Validador FluentValidation | Reglas ContentType `.pdf` / size | Mensajes de validación definidos | Código presente (no E2E upload HTTP en esta sesión) | OK* | Estática / validación | `UploadDocumentCommandValidator` + UI `DocumentsPage.tsx` |
| CP-18 | Procesamiento | Matriz de idempotencia por status | Status Uploaded/Processed/Processing | `ShouldProcess` / outcomes | Solo estados elegibles se procesan; processed se omite | Superado | OK | Unitaria | `DocumentProcessingRulesTests` |
| CP-19 | Procesamiento / RabbitMQ | Consumer documento: already processed no lanza | Outcome skipped | Consume evento | Completa sin throw | Superado | OK | Integración | `DocumentUploadedEventConsumerTests.Already_processed_outcome_does_not_throw` |
| CP-20 | RabbitMQ | Retry hasta límite luego rechaza | Fallos transientes | `MessageRetryExecutor` | Reintentos y rechazo controlado | Superado | OK | Unitaria | `MessageRetryExecutorTests` |
| CP-21 | RabbitMQ | Routing keys documento/ticket | Eventos conocidos | Resolver routing key | Keys esperadas; desconocido → null | Superado | OK | Unitaria | `MessagingRoutingKeysTests` |
| CP-22 | Tickets | Agent crea ticket de su empresa | Actor Agent | CreateTicket | Ticket creado | Superado | OK | Aplicación | `TicketCommandTests.Agent_creates_ticket_for_own_company` |
| CP-23 | Tickets | CompanyId externo rechazado | Body con otra empresa | CreateTicket | Rechazo | Superado | OK | Aplicación | `External_CompanyId_is_rejected` |
| CP-24 | Tickets | Aislamiento entre empresas | Ticket empresa B | Agent A consulta/gestiona | Sin acceso | Superado | OK | Aplicación | `Agent_company_A_cannot_see_ticket_of_company_B`, `GetTicketById_respects_company_isolation` |
| CP-25 | Tickets / escalamiento | Escalamiento idempotente | Ticket ya escalado | Preparar escalamiento | Idempotente | Superado | OK | Integración | `TicketEscalationServiceTests.Is_idempotent_when_already_processed` |
| CP-26 | Chat RAG | Persistencia conversación/mensajes | ChatDb independiente | Crear conversación y mensajes | Persistidos | Superado | OK | Integración | `ChatMicroserviceTests.Persists_conversation_and_messages_independently` |
| CP-27 | Conversaciones | Aislamiento por CompanyId | Dos empresas | Listar/leer conversaciones | Sin cruce de tenant | Superado | OK | Integración | `Isolates_conversations_by_company`, `User_cannot_read_other_company_conversation` |
| CP-28 | Chat RAG | Core no disponible → 503 | HttpClient falla | Llamada a Core | 503 controlado | Superado | OK | Integración | `Returns_503_when_core_unreachable` |
| CP-29 | Chat RAG | Gemini no configurado | Sin API key | Ask | Error controlado | Superado | OK | Integración | `Gemini_not_configured_returns_controlled_error` |
| CP-30 | Chat / Core gate | Modo External deshabilita chat embebido | `CHAT_SERVICE_MODE=External` | Evaluar gate | Embedded deshabilitado | Superado | OK | Unitaria | `EmbeddedChatGateTests.External_mode_disables_embedded_chat` |
| CP-31 | GraphQL BFF | Sin token rechazado | Bff TestServer | Query GraphQL | Rechazo auth | Superado | OK | Integración | `BffGraphQlTests.Graphql_without_token_is_rejected` |
| CP-32 | GraphQL BFF | Con token válido opera | Token de prueba | Query | Respuesta OK | Superado | OK | Integración | `Graphql_with_valid_token_works` |
| CP-33 | GraphQL BFF | Aislamiento documentos/conversaciones/tickets | Dos empresas | Queries GraphQL | Solo datos de la empresa | Superado | OK | Integración | `Documents_respect_company_isolation`, `Conversations_respect_company_isolation`, `Tickets_respect_company_isolation` |
| CP-34 | GraphQL BFF | Core/Chat caídos → error controlado | Downstream down | Query | Error controlado (no crash) | Superado | OK | Integración | `Core_api_down_returns_controlled_error`, `Chat_api_down_returns_controlled_error` |
| CP-35 | GraphQL BFF | Forward Authorization sin loguear token | Logger capturado | Forward header | Header reenviado; token no en logs | Superado | OK | Integración | `AuthHeaderForwardingTests` |
| CP-36 | Health checks | Api / Chat / Bff responden Healthy | Contenedores locales Up | `GET /health` | HTTP 200 + cuerpo Healthy | api 158 ms, chat 45 ms, bff 30 ms | OK | Humo | `artifacts/test-results/health-timings.txt` |
| CP-37 | Frontend | Build de producción | `package-lock.json` | `npm ci` + `npm run build` | Bundle generado | EXIT 0; vite ~5.67 s | OK | Build | `artifacts/test-results/npm-build-summary.txt` |
| CP-38 | Docker | Compose válido y servicios Up | Docker Desktop disponible | `docker compose config --quiet`; `docker compose ps` | Config OK; contenedores Up | COMPOSE_EXIT 0; 8 servicios Up | OK | Infra local | `artifacts/test-results/docker-ps.txt` |

\*CP-17: las reglas están implementadas y se verificaron en código; no se ejecutó en esta sesión un upload HTTP real contra la API con archivo adjunto.

## Observaciones

- El total de la suite (131) supera el número de filas de esta tabla: aquí se documentan casos trazables por módulo pedido, no un inventario exhaustivo de cada método.
- `ContactCenterAI.Domain.Tests` contiene un único assert trivial (`Assert.True(true)`); no se presentan como casos de negocio de dominio.
- CI (`.github/workflows/ci.yml`) ejecuta la misma cadena build/test/frontend en Ubuntu.
