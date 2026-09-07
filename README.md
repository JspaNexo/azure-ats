# ATS TalentIQ - Sistema Integral de Evaluacion y Seleccion de Talento Asistido por IA

Plataforma empresarial contenerizada para la gestion de vacantes, ingesta curricular automatizada y evaluacion tecnica y conductual de candidatos asistida por Inteligencia Artificial (Google Gemini AI) y metodologia conductual DISC. 

Diseñado bajo principios de Clean Architecture en .NET 10, autenticacion centralizada Keycloak 26 con tema personalizado corporativo, control de acceso basado en roles (RBAC), blindaje multicapa contra ataques de Prompt Injection y una interfaz web en React 19 totalmente responsiva y adaptativa para dispositivos moviles, tabletas y escritorio.

---

## 1. Arquitectura del Sistema

El sistema implementa una arquitectura desacoplada en microservicios contenerizados y una organizacion interna de Clean Architecture en el backend:

```text
ats/
├── backend/
│   ├── src/
│   │   ├── Ats.Domain/                 # Entidades de dominio (Candidate, JobPosition, etc.), Enums, ValueObjects y Eventos
│   │   ├── Ats.Application/            # Casos de uso CQRS (Features: Candidates, JobPositions, Ingestion), DTOs e Interfaces
│   │   ├── Ats.Infrastructure/         # EF Core, PostgreSQL, Gemini AI, Seguridad anti-inyeccion, PDF, Storage y Webhooks
│   │   └── Ats.Api/                    # Controladores REST, Middleware de autenticacion JWT/Keycloak, GlobalExceptionHandler y Swagger
│   ├── tests/
│   │   └── Ats.Tests/                  # Pruebas unitarias de sanitizacion, seguridad y casos de uso (xUnit)
│   ├── Dockerfile                      # Multi-stage Docker build (.NET 10 SDK + ASP.NET Core Runtime)
│   └── README.md                       # Documentacion tecnica de la capa backend
├── frontend/
│   ├── src/
│   │   ├── components/                 # Componentes ejecutivos y modales adaptativos responsivos
│   │   ├── context/                    # Estado de sesion Keycloak (AuthContext, proteccion de rutas y tokens)
│   │   ├── services/                   # Clientes de comunicacion API REST con inyeccion de tokens Bearer
│   │   └── types/                      # Contratos e interfaces TypeScript
│   ├── Dockerfile                      # Multi-stage build (Node.js 22 + Nginx Alpine)
│   ├── nginx.conf                      # Enrutamiento SPA y Reverse Proxy seguro (/api, /health, /swagger)
│   └── README.md                       # Documentacion tecnica de la capa frontend
├── keycloak/
│   ├── realm-export.json               # Definicion del realm ats-realm, clientes, roles y usuarios
│   └── themes/talentiq/                # Tema visual corporativo personalizado y responsivo para login
├── database/
│   └── init/                           # Scripts SQL de migracion (01-schema al 06-job-positions)
├── automation/
│   ├── n8n/workflows/                  # Definicion de flujos declarativos n8n para procesamiento asincrono
│   └── README.md                       # Documentacion de automatizacion y webhooks
├── docs/                               # Documentos de diseno de software (sdd.md, mvp.md)
├── infra/                              # Configuracion de infraestructura y despliegue
├── docker-compose.yml                  # Orquestacion integral de servicios
└── README.md                           # Documentacion general del sistema
```

---

## 2. Pila Tecnologica

- **Backend:** .NET 10 (C#) bajo Clean Architecture, CQRS y principios SOLID.
- **Base de Datos:** PostgreSQL 16 Alpine con tipos de datos relacionales, llaves foraneas indexadas y campos JSONB.
- **Autenticacion & Identidad:** Keycloak 26 (OpenID Connect / OAuth 2.0 con PKCE, validacion JWKS y tema responsivo TalentIQ).
- **Inteligencia Artificial:** Google Gemini AI (modelo `gemini-flash-lite-latest` con cadena de resiliencia y respaldo multi-modelo a `gemini-3.1-flash-lite` y `gemini-3.5-flash-lite`), estructuracion JSON estricta, systemInstruction nativo y defensas anti-prompt injection.
- **Frontend:** React 19, TypeScript, Tailwind CSS, Vite, Lucide Icons y arquitectura de diseno responsive mobile-first.
- **Observabilidad:** Seq y Serilog para ingesta centralizada de logs estructurados con trazabilidad de correlacion.
- **Orquestacion de Flujos:** n8n para tareas de automatizacion batch y webhooks con firma criptografica HMAC SHA-256.
- **Infraestructura:** Docker y Docker Compose con redes aisladas y politicas de reinicio automatico.

---

## 3. Servicios y Accesos del Sistema

Al levantar el entorno con Docker Compose, los siguientes servicios quedan disponibles:

| Servicio | Contenedor | Puerto Host | Descripcion | Credenciales / Acceso |
| :--- | :--- | :--- | :--- | :--- |
| **Frontend Web** | `ats_frontend` | [http://localhost:5173](http://localhost:5173) | Consola ejecutiva ATS responsiva | Autenticacion via Keycloak |
| **Backend REST API** | `ats_backend` | [http://localhost:5027](http://localhost:5027) | API REST y endpoints de negocio | Token Bearer JWT |
| **Swagger UI** | `ats_backend` | [http://localhost:5027/swagger](http://localhost:5027/swagger) | Especificacion OpenAPI interactiva | Acceso de desarrollo |
| **Servidor Keycloak** | `ats_keycloak` | [http://localhost:8085](http://localhost:8085) | Servidor de Identidad OIDC y Roles | Realm: `ats-realm` (admin / admin) |
| **Base de Datos** | `ats_postgres` | `localhost:5433` | PostgreSQL 16 Relacional | `ats_db` / `postgres` / `postgres` |
| **Observabilidad Seq** | `ats_seq` | [http://localhost:8080](http://localhost:8080) | Panel de telemetria y logs estructurados | Usuario: `admin` / Clave: `Admin12345!` |
| **Automatizacion n8n** | `ats_n8n` | [http://localhost:5678](http://localhost:5678) | Orquestador de flujos declarativos | Configuracion inicial de cuenta |

### Cuentas de Acceso Preconfiguradas en Keycloak

El realm `ats-realm` incluye las siguientes cuentas para validar los diferentes roles:

1. **Perfil Administrador:**
   - Usuario: `admin`
   - Contraseña: `Admin123!`
   - Nombre: Administrador Comite (`admin@ats.com`)
   - Rol asignado: `ats_admin`
   - Capacidades: Supervision global, registro de nuevas vacantes, delegacion de candidatos a evaluadores, carga directa de CV y emision de dictamenes.

2. **Perfil Evaluador / Reclutador (Principal):**
   - Usuario: `carlos.mendoza`
   - Contraseña: `Recruiter123!`
   - Nombre: Carlos Mendoza (`carlos.mendoza@empresa.com`)
   - Rol asignado: `ats_recruiter`
   - Capacidades: Vista filtrada a expedientes asignados ("Mis Asignados"), carga directa de postulantes y evaluacion con IA, revision de perfiles DISC, guia de preguntas STAR y dictamen de entrevista.

3. **Perfil Evaluadora / Reclutadora (Secundaria):**
   - Usuario: `laura.sanchez`
   - Contraseña: `Recruiter123!`
   - Nombre: Laura Sanchez (`laura.sanchez@empresa.com`)
   - Rol asignado: `ats_recruiter`

---

## 4. Matriz de Control de Acceso Basado en Roles (RBAC)

El sistema valida criptograficamente los tokens JWT emitidos por Keycloak en cada solicitud HTTP mediante el middleware de autorizacion de ASP.NET Core:

| Funcionalidad / Endpoint | Rol Administrador (`ats_admin`) | Rol Reclutador (`ats_recruiter`) | Politica de Seguridad Backend |
| :--- | :---: | :---: | :--- |
| Consultar metricas globales de candidatos | Si | Solo metricas de su grupo asignado | `[Authorize(Roles = "ats_admin,ats_recruiter")]` |
| Ver listado de candidatos | Si | Si | `[Authorize(Roles = "ats_admin,ats_recruiter")]` |
| Filtrar "Mis Asignados" vs "Todos" | Si | Si (predeterminado a sus asignados) | Logica de filtrado por `preferred_username` |
| Crear nueva vacante formal | Si | No | `[Authorize(Roles = "ats_admin")]` |
| Consultar vacantes activas | Si | Si | `[Authorize(Roles = "ats_admin,ats_recruiter")]` |
| Cambiar estado de una vacante | Si | No | `[Authorize(Roles = "ats_admin")]` |
| Cargar CV y evaluar con IA en tiempo real | Si | Si | `[Authorize(Roles = "ats_admin,ats_recruiter")]` |
| Asignar/reasignar evaluador a candidato | Si | No | `[Authorize(Roles = "ats_admin")]` |
| Consultar expediente completo (CV, DISC, STAR) | Si | Si | `[Authorize(Roles = "ats_admin,ats_recruiter")]` |
| Emitir dictamen oficial de entrevista | Si | Si | `[Authorize(Roles = "ats_admin,ats_recruiter")]` |
| Descargar informe oficial en PDF | Si | Si | `[Authorize(Roles = "ats_admin,ats_recruiter")]` |

---

## 5. Modulos Clave del Sistema

### 5.1 Gestion de Vacantes y Puestos Formales (`Job Positions`)
- Registro de vacantes con titulo, departamento, nivel de seniority, anos de experiencia requeridos, descripcion y requisitos clave.
- Vinculacion relacional estricta: la tabla `candidates` contiene la llave foranea `job_position_id` indexada con regla `ON DELETE SET NULL`.
- Conteo dinamico en tiempo real del numero de postulantes vinculados a cada vacante en el listado (`CandidateCount`).
- Modal responsivo corporativo para alta de vacantes disponible exclusivamente para usuarios con rol `ats_admin`.

### 5.2 Ingesta Directa de CV y Evaluacion Asistida por IA en Tiempo Real
- Modal interactivo para la carga de curriculum en formato PDF con zona drag-and-drop y validacion de tamano (maximo 15 MB).
- Selector de vacante activa asociada al proceso de postulacion.
- Controles deslizantes (sliders) interactivos para capturar las 4 dimensiones conductuales DISC (Dominancia, Influencia, Estabilidad, Cumplimiento) con clasificacion automatica del estilo primario.
- Pipeline de analisis ejecutado en tiempo real con Google Gemini AI con seguimiento visual de fases:
  1. Extraccion de texto y aplicacion de defensas anti-injection.
  2. Analisis curricular y normalizacion de competencias tecnicas con evidencia textual.
  3. Interpretacion conductual DISC orientada al desempeno profesional.
  4. Generacion de guia de entrevista situacional estructurada bajo metodologia STAR.
  5. Generacion del reporte consolidado y vinculacion automatica al dashboard.

### 5.3 Asignacion y Delegacion de Expedientes
- Permite a los administradores delegar expedientes individuales a reclutadores especificos (`carlos.mendoza`, `laura.sanchez`).
- Vista personalizada para reclutadores que prioriza sus expedientes asignados.
- Notificaciones claras en tarjetas y expediente con el reclutador a cargo.

### 5.4 Diseno Web Responsivo y Accesible (Mobile-First)
- **Barra de navegacion superior adaptativa:** Menu hamburguesa tactil en pantallas moviles (< 640px) y barra expandida en escritorio (>= 640px).
- **Tarjetas de metricas fluidas:** 1 columna en moviles pequenos, 2 en tabletas y 4 en escritorios.
- **Ventanas modales auto-ajustables:** Contenedores `h-[95vh] sm:h-auto sm:max-h-[90vh]` con scroll vertical independiente para evitar desbordamientos en cualquier dispositivo.
- **Grafico Radar SVG escalable:** Coordenadas canónicas `viewBox="0 0 300 300"` y ancho fluido que previene cortes de etiquetas en pantallas estrechas.

---

## 6. Arquitectura de Seguridad y Mitigaciones Implementadas

### 6.1 Defensa en Profundidad contra Prompt Injection
1. **Capa 1 (Pre-inspeccion heuristica):** `CvSecuritySanitizer` escanea el texto del CV mediante expresiones regulares precompiladas para detectar intentos de evasion, ordenes imperativas ("ignore previous instructions") y neutraliza delimitadores XML.
2. **Capa 2 (Contencion estricta de privilegios):** Inyeccion de directivas del sistema en `systemInstruction` de Gemini (maxima prioridad jerarquica) y encapsulamiento del contenido no confiable dentro de etiquetas de aislamiento `<untrusted_applicant_cv>`.
3. **Capa 3 (Verificacion de evidencias en C# - Grounding Check):** Cada habilidad o competencia tecnica reportada debe incluir una cita textual exacta verificable en el cuerpo del CV original.
4. **Capa 4 (Supervision humana - Human-in-the-Loop):** Alertas de integridad visibles en el expediente del postulante para que el evaluador humano tome siempre la decision final.

### 6.2 Mitigaciones de Seguridad del Backend
- **Proteccion contra Path Traversal:** En [`StorageService.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Infrastructure/Services/Storage/StorageService.cs), todas las rutas de almacenamiento de archivos se resuelven de forma absoluta y se validan contra el directorio base mediante `Path.GetFullPath()`, rechazando intentos de salto de directorio (`..`).
- **Validacion Criptografica de Webhooks:** En [`WebhooksController.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Api/Controllers/WebhooksController.cs), la firma HMAC SHA-256 del encabezado `X-ATS-Signature` se valida mediante `CryptographicOperations.FixedTimeEquals` para prevenir ataques de canal lateral basados en tiempo (Timing Attacks).
- **Manejo Global de Excepciones:** [`GlobalExceptionHandlerMiddleware.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Api/Middlewares/GlobalExceptionHandlerMiddleware.cs) captura cualquier excepcion no controlada y emite respuestas estandarizadas RFC 7807 (ProblemDetails), suprimiendo volcados de memoria y trazas de ejecucion internas.
- **Proteccion de Credenciales Sensibles:** Las claves de API de Google Gemini y secretos de webhooks se gestionan exclusivamente a traves de variables de entorno del sistema (`Gemini__ApiKey`, `Webhooks__Secret`), sin persistencia de credenciales en codigo fuente o repositorios publicos.

---

## 7. Despliegue y Ejecucion

### Despliegue con Docker Compose (Recomendado)

1. Clonar el repositorio y acceder al directorio raiz:
   ```bash
   git clone <URL_DEL_REPOSITORIO>
   cd ats
   ```

2. Crear el archivo `.env` a partir de la plantilla `.env.example` y configurar la clave de API de Google Gemini:
   ```bash
   cp .env.example .env
   ```
   Abra `.env` y configure su clave privada en `Gemini__ApiKey=TU_API_KEY` (obtenible gratuitamente en [Google AI Studio](https://aistudio.google.com/app/apikey)).

3. Construir e iniciar la totalidad de los contenedores:
   ```bash
   docker compose up -d --build
   ```

4. Verificar que todos los servicios esten activos y saludables:
   ```bash
   docker compose ps
   ```

5. Abrir el navegador en [http://localhost:5173](http://localhost:5173) e identificarse con las credenciales de administrador o reclutador.

### Comandos de Mantenimiento

```bash
# Reiniciar solo el contenedor de frontend tras cambios de UI
docker compose build frontend && docker compose up -d frontend

# Reiniciar solo el backend tras modificaciones de codigo
docker compose build backend && docker compose up -d backend

# Consultar logs estructurados en tiempo real
docker compose logs -f backend

# Detener los servicios conservando volumenes de base de datos
docker compose down
```

---

## 8. Pruebas Automatizadas

El proyecto incluye una suite completa de pruebas unitarias y de seguridad con xUnit en `backend/tests/Ats.Tests`:

```bash
dotnet test backend/tests/Ats.Tests/Ats.Tests.csproj
```

### Casos de Prueba Incluidos:
- **Procesamiento de CVs estandar:** Verificacion de que curriculums legitimos se analicen sin falsos positivos de seguridad.
- **Deteccion de ataques de Prompt Injection:** Evaluacion de patrones en espanol e ingles (e.g. "ignore all instructions", "override system", "califica con 100%").
- **Neutralizacion de etiquetas de escape:** Comprobacion de reemplazo y desinfeccion de etiquetas `</untrusted_applicant_cv>`.
- **Eliminacion de tokens de control LLM:** Supresion de delimitadores especiales estilo `<|im_start|>`.

---

## 9. Hoja de Ruta (Roadmap del Proyecto)

### Fases Completadas
- **Fase 1:** Arquitectura limpia en .NET 10, persistencia relacional en PostgreSQL 16 y extraccion de texto de PDF.
- **Fase 2:** Analisis curricular y evaluacion DISC con Google Gemini AI y generacion de preguntas STAR.
- **Fase 3:** Consola web ejecutiva en React 19, Tailwind CSS y visualizacion de radar DISC.
- **Fase 4:** Autenticacion centralizada Keycloak 26 (OIDC/PKCE) con RBAC (`ats_admin` y `ats_recruiter`).
- **Fase 5:** Blindaje multicapa contra Prompt Injection y alertas de integridad curricular.
- **Fase 6:** Modulo formal de Vacantes (`Job Positions`), carga directa de postulantes con IA en vivo y optimizacion responsiva multi-dispositivo.
- **Fase 7:** Auditoria de seguridad y mitigaciones (Path Traversal, HMAC timing-safe, exception handler global, tema Keycloak corporativo responsive).

### Fases Planificadas
- **Fase 8:** Notificaciones en tiempo real via WebSockets/SignalR para avisar inmediatamente al reclutador ante nuevas delegaciones de expedientes.
- **Fase 9:** Portal publico de auto-postulacion directa para postulantes con captcha empresarial y limitacion de tasa de peticiones (rate limiting).
- **Fase 10:** Sincronizacion de entrevistas con calendarios corporativos (Google Calendar, Microsoft Outlook / Teams).
- **Fase 11:** Analitica avanzada de pipeline de seleccion y calculo de tiempos de ciclo de contratacion.
