# Documentación — ContactCenterAI

Índice de la documentación del repositorio.

## Estructura

| Carpeta | Contenido |
|---------|-----------|
| [architecture/](architecture/) | Arquitectura vigente (Auth0, Chat, despliegue, ownership de datos) |
| [api/](api/) | Ejemplos de API (GraphQL BFF) |
| [security/](security/) | Autenticación y autorización |
| [testing/](testing/) | Guías de prueba (si aplica) |
| [sumativa-2/](sumativa-2/) | Evidencias de calidad Sumativa 2 |
| [evidence/](evidence/) | Evidencias técnicas y material histórico |
| [academic/](academic/) | Entregables académicos por semana (Word/PDF) |
| [adr/](adr/) | Architecture Decision Records (reservado) |

## Arquitectura vigente

- [Integración Auth0](architecture/auth0-integration.md)
- [Microservicio Chat/RAG](architecture/chat-microservice.md)
- [Despliegue multi-servicio](architecture/microservices-deployment.md)
- [Propiedad de datos por servicio](architecture/service-data-ownership.md)

## API

- [Ejemplos GraphQL BFF](api/graphql-bff-examples.md)

## Seguridad

- [Autenticación y autorización](security/authentication-authorization.md)

## Sumativa 2

- [Índice Sumativa 2](sumativa-2/README.md)

## Evidencias

- [Evidencia de pruebas Chat](evidence/chat-microservice-test-evidence.md)
- [Históricos (planes y reportes previos)](evidence/historico/) — no describen el sistema actual

## Entrega final

| Documento | Ruta |
|-----------|------|
| Manual técnico | [final/Manual_Tecnico.md](final/Manual_Tecnico.md) |
| Manual de usuario | [final/Manual_Usuario.md](final/Manual_Usuario.md) |
| README final | [final/README_Final.md](final/README_Final.md) |
| Scripts SQL | [`../scripts/`](../scripts/) |
| Docker entrega | [`../deployment/Docker/`](../deployment/Docker/) |

## Académico

Colocar entregables Word/PDF en:

- `academic/semana-3/` … `academic/semana-8/`
