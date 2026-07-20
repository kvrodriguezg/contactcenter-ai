# Manual de Usuario — ContactCenterAI

**Sistema:** ContactCenterAI  
**Audiencia:** Agentes, administradores de empresa y superadministradores  
**Versión:** 1.0 (entrega final académica)  

Este manual explica, paso a paso, cómo usar la plataforma desde el navegador.  
No requiere conocimientos técnicos de programación.

---

## Tabla de contenidos

1. [¿Qué es ContactCenterAI?](#1-qué-es-contactcenterai)
2. [Requisitos para usar el sistema](#2-requisitos-para-usar-el-sistema)
3. [Roles de usuario](#3-roles-de-usuario)
4. [Inicio de sesión (Login)](#4-inicio-de-sesión-login)
5. [Dashboard](#5-dashboard)
6. [Gestión de empresas](#6-gestión-de-empresas)
7. [Gestión de usuarios](#7-gestión-de-usuarios)
8. [Gestión de documentos](#8-gestión-de-documentos)
9. [Subir un PDF](#9-subir-un-pdf)
10. [Esperar el procesamiento](#10-esperar-el-procesamiento)
11. [Chat IA](#11-chat-ia)
12. [Historial de conversaciones](#12-historial-de-conversaciones)
13. [Tickets y escalamiento](#13-tickets-y-escalamiento)
14. [Administración y resumen GraphQL](#14-administración-y-resumen-graphql)
15. [Cierre de sesión](#15-cierre-de-sesión)
16. [Preguntas frecuentes](#16-preguntas-frecuentes)
17. [Glosario](#17-glosario)

---

## 1. ¿Qué es ContactCenterAI?

ContactCenterAI es una plataforma de apoyo para agentes de contact center. Permite:

- Organizar el trabajo por **empresa** (cliente o tenant).
- Cargar **documentos PDF** (manuales, políticas, guías).
- Consultar esos documentos con un **asistente de inteligencia artificial** (Chat IA).
- Ver las **fuentes** de cada respuesta (qué documento se usó).
- Crear **tickets** cuando una consulta debe escalarse a un humano.

> **Idea clave:** primero se cargan y procesan los PDF; después el Chat IA responde con base en esos documentos.

---

## 2. Requisitos para usar el sistema

| Requisito | Detalle |
|-----------|---------|
| Navegador | Chrome, Edge, Firefox o similar actualizado |
| Acceso | URL del sistema (por ejemplo `http://localhost:5173` en desarrollo) |
| Cuenta | Usuario creado por un administrador |
| Red | Conexión a Internet (necesaria para el asistente IA) |

![Captura — Pantalla de acceso](../assets/capturas/01-login.png)  
*(Placeholder: inserte aquí una captura de la pantalla de login.)*

---

## 3. Roles de usuario

| Rol | Qué puede hacer |
|-----|-----------------|
| **SuperAdmin** | Gestionar todas las empresas y usuarios; ver todo el sistema |
| **CompanyAdmin** | Gestionar usuarios de su empresa; documentos, chat y tickets de su empresa |
| **Agent** | Subir/consultar documentos, usar Chat IA y tickets de su empresa |

El menú lateral muestra solo las opciones disponibles según su rol.

---

## 4. Inicio de sesión (Login)

Hay dos formas de entrar, según cómo esté configurado el sistema.

### 4.1 Login con correo y contraseña (modo Local / desarrollo)

1. Abra la URL del sistema.
2. En la pantalla **ContactCenterAI**, escriba su **correo electrónico**.
3. Escriba su **contraseña**.
4. Pulse **Iniciar sesión**.
5. Si los datos son correctos, ingresará al **Dashboard**.

![Captura — Login local](../assets/capturas/02-login-local.png)  
*(Placeholder)*

#### Credenciales de demostración (solo desarrollo)

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| admin@contactcenterai.cl | Admin123* | SuperAdmin |
| agente@contactcenterai.cl | Agent123* | Agent |

> Estas credenciales **no** aplican cuando el sistema usa Auth0 en producción.

### 4.2 Login con Auth0 (producción / modo Auth0)

1. Abra la URL del sistema.
2. Pulse **Ingresar con Auth0**.
3. Complete el inicio de sesión en la página de Auth0.
4. Será redirigido de vuelta a ContactCenterAI.

![Captura — Login Auth0](../assets/capturas/03-login-auth0.png)  
*(Placeholder)*

### Si no puede entrar

| Mensaje / situación | Qué hacer |
|---------------------|-----------|
| Credenciales incorrectas | Verifique correo y contraseña |
| Usuario no registrado (Auth0) | Pida a un administrador que lo cree en **Usuarios** con el mismo correo |
| Usuario inactivo | Solicite reactivación a un administrador |
| El login local no aparece / error 410 | El sistema está en Auth0: use el botón Auth0 |

---

## 5. Dashboard

Al iniciar sesión verá la página de bienvenida.

### Qué muestra

- Su **correo**.
- Su **rol**.
- Su **empresa** (si aplica).
- Accesos rápidos a módulos: Documentos, Chat IA, Tickets, etc.

### Cómo navegar

Use el **menú izquierdo**:

| Opción | Descripción |
|--------|-------------|
| Dashboard | Inicio |
| Empresas | Solo administradores |
| Usuarios | Solo administradores |
| Resumen GQL | Solo administradores |
| Documentos | Base de conocimiento PDF |
| Chat IA | Asistente y historial |
| Tickets | Escalamiento |

En la barra superior verá su correo, rol y el botón **Cerrar sesión**.

![Captura — Dashboard](../assets/capturas/04-dashboard.png)  
*(Placeholder)*

---

## 6. Gestión de empresas

**Quién:** principalmente **SuperAdmin**.

### 6.1 Ver empresas

1. Menú → **Empresas**.
2. Verá una tabla con nombre, estado (Activa / Inactiva) y acciones.

### 6.2 Crear una empresa

1. Pulse **Nueva empresa** (o botón equivalente de creación).
2. Escriba el **nombre**.
3. Seleccione el **estado** (Active / Inactive).
4. Guarde.
5. Confirme el mensaje de éxito.

### 6.3 Editar una empresa

1. En la fila de la empresa, pulse el ícono de **editar**.
2. Modifique el nombre o estado.
3. Guarde.

### 6.4 Activar / desactivar

Use los íconos de activar o pausar en la fila correspondiente.  
Una empresa inactiva no debería usarse para operaciones normales de agentes.

![Captura — Empresas](../assets/capturas/05-empresas.png)  
*(Placeholder)*

---

## 7. Gestión de usuarios

**Quién:** **SuperAdmin** y **CompanyAdmin**.

### 7.1 Ver usuarios

1. Menú → **Usuarios**.
2. Revise correo, nombre, rol, empresa, estado y proveedor (Local / Auth0).

### 7.2 Crear un usuario

1. Pulse **Nuevo usuario**.
2. Complete:
   - Correo electrónico
   - Nombre
   - Rol (`SuperAdmin`, `CompanyAdmin` o `Agent`)
   - Empresa (obligatoria para agentes y admins de empresa)
   - Contraseña (modo Local) **o** datos de Auth0 según configuración
3. Guarde.

> En modo Auth0, el correo del usuario en ContactCenterAI debe coincidir con el de Auth0.

### 7.3 Editar / activar / desactivar

1. Use **editar** para cambiar datos o rol.
2. Use activar/desactivar para permitir o bloquear el acceso sin borrar la cuenta.

![Captura — Usuarios](../assets/capturas/06-usuarios.png)  
*(Placeholder)*

---

## 8. Gestión de documentos

**Quién:** todos los usuarios autenticados (según su empresa).

1. Menú → **Documentos**.
2. Verá la lista de PDF cargados con:
   - Nombre del archivo
   - Tamaño
   - Estado de procesamiento
   - Fecha

### Estados posibles

| Estado en pantalla | Significado |
|--------------------|-------------|
| Subido | Archivo recibido |
| Pendiente | En cola para procesar |
| Procesando | El sistema está indexando el contenido |
| Procesado | Listo para usarse en el Chat IA |
| Fallido | Hubo un error; revise o vuelva a subir |

![Captura — Lista de documentos](../assets/capturas/07-documentos.png)  
*(Placeholder)*

---

## 9. Subir un PDF

1. Vaya a **Documentos**.
2. Si es SuperAdmin, seleccione la **empresa** destino cuando el sistema lo solicite.
3. Pulse el botón para **seleccionar / subir archivo**.
4. Elija un archivo **PDF**.
5. Espere el mensaje de confirmación.

### Reglas prácticas

| Regla | Detalle |
|-------|---------|
| Formato | Solo PDF |
| Tamaño máximo | 10 MB (según la interfaz) |
| Contenido | Preferir texto seleccionable (no solo imagen escaneada sin OCR) |
| Idioma | El asistente responde mejor si la pregunta y el documento están alineados |

![Captura — Subida de PDF](../assets/capturas/08-subir-pdf.png)  
*(Placeholder)*

---

## 10. Esperar el procesamiento

Después de subir:

1. El documento aparece como **Pendiente** o **Procesando**.
2. Un servicio en segundo plano (Worker) lee el PDF, lo divide en fragmentos y genera representaciones semánticas.
3. Cuando el estado pase a **Procesado**, ya puede usarlo en el Chat IA.
4. Si queda en **Fallido**, informe a soporte técnico o reintente con otro archivo.

### Consejos

- No es necesario “reiniciar” la página de inmediato; puede actualizar la lista después de unos segundos.
- Si hay muchos documentos, el procesamiento puede demorar más.
- Sin clave de IA configurada en el servidor, el procesamiento puede fallar (esto lo resuelve el administrador del sistema).

![Captura — Documento procesado](../assets/capturas/09-documento-procesado.png)  
*(Placeholder)*

---

## 11. Chat IA

**Quién:** agentes y administradores autenticados.

### 11.1 Abrir el chat

1. Menú → **Chat IA**.
2. Verá el panel de conversación y, normalmente, el listado de conversaciones previas.

### 11.2 Hacer una pregunta

1. Escriba su pregunta en el cuadro de texto (por ejemplo: *“¿Cuál es el horario de atención del plan fibra?”*).
2. Pulse **Enviar**.
3. Espere la respuesta del asistente.

### 11.3 Revisar las fuentes

Debajo de la respuesta pueden aparecer **Fuentes consultadas**:

- Nombre del documento
- Fragmento (chunk)
- Porcentaje de similitud
- Vista previa del texto usado

Expanda cada fuente para verificar que la respuesta se basa en el material correcto.

### 11.4 Buenas prácticas al preguntar

| Hacer | Evitar |
|-------|--------|
| Preguntas concretas sobre los PDF cargados | Preguntar datos que no están en los documentos |
| Una pregunta a la vez | Pegar textos enormes sin contexto |
| Revisar fuentes | Confiar ciegamente sin contrastar |

Si no hay información suficiente, el asistente indicará que no encontró datos en los documentos.

![Captura — Chat IA](../assets/capturas/10-chat-ia.png)  
*(Placeholder)*

### 11.5 Crear un ticket desde el chat

Si la respuesta no resuelve el caso:

1. Use la opción para **crear ticket** (cuando esté disponible en la pantalla).
2. Complete asunto, descripción y prioridad (Baja, Media, Alta, Crítica).
3. Guarde. El ticket quedará asociado al seguimiento humano.

---

## 12. Historial de conversaciones

El historial vive en la misma pantalla de **Chat IA**.

### Cómo usarlo

1. En el listado lateral (o panel de historial), seleccione una conversación anterior.
2. Se cargarán los mensajes previos.
3. Puede continuar la conversación o iniciar una **nueva**.

Desde el **Dashboard**, el acceso rápido **Historial** también lo lleva al Chat.

![Captura — Historial](../assets/capturas/11-historial.png)  
*(Placeholder)*

---

## 13. Tickets y escalamiento

**Quién:** usuarios autenticados (visibilidad según empresa/rol).

1. Menú → **Tickets**.
2. Verá tickets con asunto, prioridad, estado y asignación.

### Estados habituales

| Estado | Significado |
|--------|-------------|
| Pendiente | Recién creado / sin resolver |
| En revisión | En análisis |
| Resuelto | Con resolución registrada |
| Cerrado | Finalizado |

### Acciones típicas

- **Crear** un ticket nuevo.
- **Asignar** a un usuario.
- **Cambiar estado**.
- **Resolver** con texto de resolución.

![Captura — Tickets](../assets/capturas/12-tickets.png)  
*(Placeholder)*

---

## 14. Administración y resumen GraphQL

**Quién:** SuperAdmin / CompanyAdmin.

Además de Empresas y Usuarios, el menú **Resumen GQL** muestra información agregada obtenida a través del BFF GraphQL (vista de resumen de empresa, usuarios, documentos, etc., según la pantalla implementada).

Úsela para:

- Verificar de un vistazo el estado de una empresa.
- Contrastar datos sin entrar módulo por módulo.

![Captura — Resumen administración](../assets/capturas/13-admin-resumen.png)  
*(Placeholder)*

---

## 15. Cierre de sesión

1. En la barra superior, pulse **Cerrar sesión**.
2. Volverá a la pantalla de login.
3. En modo Auth0, también se cierra la sesión del proveedor según la configuración del sistema.

> Recomendación: cierre sesión al terminar, especialmente en equipos compartidos.

![Captura — Cerrar sesión](../assets/capturas/14-logout.png)  
*(Placeholder)*

---

## 16. Preguntas frecuentes

**¿Por qué el Chat no encuentra información?**  
Compruebe que el PDF esté en estado **Procesado** y que la pregunta se refiera al contenido del documento.

**¿Puedo subir Word o Excel?**  
En la versión actual la carga está orientada a **PDF**.

**¿Un agente ve documentos de otra empresa?**  
No. El sistema aísla la información por empresa.

**¿Auth0 me pide login pero falla al volver?**  
Pida a un administrador que verifique que su usuario exista y esté activo en ContactCenterAI con el mismo correo.

**¿Qué hago si un documento queda en Fallido?**  
Reintente con otro PDF o contacte al administrador técnico (posible problema de procesamiento o de configuración de IA).

---

## 17. Glosario

| Término | Significado sencillo |
|---------|----------------------|
| Tenant / Empresa | Organización cuyos datos están separados de otras |
| RAG | Técnica: buscar en documentos y luego generar la respuesta |
| Embedding | Representación numérica del texto para búsqueda por significado |
| Chunk | Fragmento de un documento |
| BFF | Capa intermedia (GraphQL) que agrupa información para la interfaz |
| Ticket | Caso de soporte escalado a un humano |
| Auth0 | Servicio externo de inicio de sesión |

---

## Contacto de soporte (plantilla)

| Campo | Valor |
|-------|-------|
| Proyecto | ContactCenterAI |
| Administrador del sistema | *(completar)* |
| Correo de soporte | *(completar)* |

---

*Fin del Manual de Usuario — ContactCenterAI*
