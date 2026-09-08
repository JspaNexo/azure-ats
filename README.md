# ATS TalentIQ - Sistema Integral de Evaluacion y Seleccion de Talento Asistido por IA

Plataforma empresarial contenerizada para la gestion de vacantes, ingesta curricular automatizada y evaluacion tecnica y conductual de candidatos asistida por Inteligencia Artificial (Google Gemini AI) y metodologia conductual DISC. 

Diseñado bajo principios de Clean Architecture en .NET 10, autenticacion centralizada Keycloak 26 con tema personalizado corporativo, control de acceso basado en roles (RBAC), blindaje multicapa contra ataques de Prompt Injection y una interfaz web en React 19 totalmente responsiva y adaptativa para dispositivos moviles, tabletas y escritorio.

> [!NOTE]
> **Aviso de Alcance del Proyecto - Prototipo / Prueba de Concepto (PoC)**
> Este software constituye un prototipo funcional y demostrativo. Los flujos de trabajo, pantallas, cuestionarios psicometricos y etapas de evaluacion aqui presentados no reflejan de manera estricta o vinculante el proceso de seleccion y reclutamiento real de ninguna empresa u organizacion. La solucion ha sido concebida con una arquitectura modular y parametrizable que facilita modificar, extender o redisenar cualquier componente (criterios de scoring, proveedores de IA, etapas de evaluacion, integraciones con ATS existentes o flujos de aprobacion) para ajustarse fielmente a las necesidades y normativas de cada organizacion.

---

## 1. Arquitectura del Sistema

El sistema implementa una arquitectura desacoplada en microservicios contenerizados y una organizacion interna de Clean Architecture en el backend:

```text
ats/
├── Ats.slnx                            # Solucion unificada .NET 10 (Backend + Suites de Pruebas)
├── backend/
│   ├── src/
│   │   ├── Ats.Domain/                 # Entidades de dominio (Candidate, JobPosition, etc.), Enums, ValueObjects y Eventos
│   │   ├── Ats.Application/            # Casos de uso CQRS (Candidates, JobPositions, Ingestion, Reports), DTOs e Interfaces
│   │   ├── Ats.Infrastructure/         # EF Core, Migraciones, Gemini AI, MockAiProvider, Storage (Local/S3), Caching y Jobs
│   │   └── Ats.Api/                    # Controladores REST, Autenticacion JWT/Keycloak, GlobalExceptionHandler y Swagger
│   ├── tests/
│   │   └── Ats.Tests/                  # Pruebas unitarias de seguridad, sanitizacion, scoring de calce y habilidades
│   ├── Dockerfile                      # Multi-stage Docker build (.NET 10 SDK + ASP.NET Core Runtime)
│   └── README.md                       # Documentacion tecnica de la capa backend
├── tests/
│   ├── Ats.Domain.UnitTests/           # Pruebas unitarias del modelo de dominio y Value Objects
│   ├── Ats.Application.UnitTests/      # Pruebas unitarias de handlers CQRS y logica de aplicacion
│   └── Ats.ArchitectureTests/          # Pruebas de cumplimiento de arquitectura limpia (dependencias entre capas)
├── frontend/
│   ├── src/
│   │   ├── components/                 # Componentes ejecutivos (dashboard, stats, modales)
│   │   │   └── evaluator/              # Pestanas modulares del expediente (CvAnalysis, DISC, STAR, Dictamen, PDF)
│   │   ├── context/                    # Estado de sesion Keycloak (AuthContext, proteccion de rutas y tokens)
│   │   ├── services/                   # Clientes de comunicacion API REST (api.ts desacoplado via VITE_API_BASE_URL)
│   │   └── types/                      # Contratos e interfaces TypeScript
│   ├── .env.example                    # Plantilla de variables de entorno frontend
│   ├── Dockerfile                      # Multi-stage build (Node.js 22 + Nginx Alpine)
│   ├── nginx.conf                      # Enrutamiento SPA y Reverse Proxy seguro (/api, /health, /swagger)
│   └── README.md                       # Documentacion tecnica de la capa frontend
├── keycloak/
│   ├── realm-export.json               # Definicion del realm ats-realm, clientes, roles y usuarios
│   └── themes/talentiq/                # Tema visual corporativo personalizado y responsivo para login
├── database/
│   ├── init/                           # Scripts SQL de migracion inicial (00 al 06-job-positions)
│   └── README.md                       # Documentacion de persistencia relacional
├── automation/
│   ├── n8n/workflows/                  # Definicion de flujos declarativos n8n para procesamiento asincrono
│   └── README.md                       # Documentacion de automatizacion y webhooks
├── docs/                               # Documentacion tecnica integral (DOCUMENTACION_TECNICA.md, sdd.md, mvp.md)
├── infra/                              # Configuracion de infraestructura y despliegue
├── docker-compose.yml                  # Orquestacion integral de servicios
└── README.md                           # Documentacion general del sistema
```

---

## 2. Pila Tecnologica

- **Backend:** .NET 10 (C#) bajo Clean Architecture, patron CQRS, FluentValidation y principios SOLID.
- **Base de Datos:** PostgreSQL 16 Alpine con tipos relacionales, llaves foraneas indexadas, campos JSONB y migraciones automaticas de EF Core (`db.Database.Migrate()`).
- **Autenticacion & Identidad:** Keycloak 26 (OpenID Connect / OAuth 2.0 con PKCE, validacion JWKS y tema responsivo TalentIQ).
- **Inteligencia Artificial:** Google Gemini AI (modelo `gemini-flash-lite-latest` con cadena de resiliencia y respaldo multi-modelo a `gemini-3.1-flash-lite` y `gemini-3.5-flash-lite`), estructuracion JSON estricta, systemInstruction nativo y defensas anti-prompt injection. Proveedor `MockAiProvider` automatico en entornos locales sin API Key.
- **Procesamiento Asincrono y Jobs:** Cola en segundo plano desacoplada `IBackgroundJobQueue` implementada con `System.Threading.Channels` y `QueuedHostedService`.
- **Almacenamiento Desacoplado:** Abstraccion `IStorageService` con implementaciones dinamicas para almacenamiento local (`LocalStorageService`) y nube S3 (`S3StorageService`).
- **Caché:** Abstraccion `ICacheService` con implementacion en memoria `MemoryCacheService` para optimizacion de consultas recurrentes.
- **Frontend:** React 19, TypeScript, Tailwind CSS, Vite, Lucide Icons y arquitectura de diseno responsive mobile-first.
- **Observabilidad:** Seq y Serilog para ingesta centralizada de logs estructurados con trazabilidad de correlacion.
- **Orquestacion de Flujos:** n8n para tareas de automatizacion batch y webhooks con firma criptografica HMAC SHA-256 (`FixedTimeEquals`).
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

El proyecto incluye una suite exhaustiva de 69 pruebas automatizadas distribuidas en cuatro proyectos bajo `Ats.slnx`:

```bash
# Ejecutar la totalidad de las pruebas en la solucion
dotnet test Ats.slnx

# O ejecutar proyectos individuales
dotnet test tests/Ats.Domain.UnitTests/Ats.Domain.UnitTests.csproj
dotnet test tests/Ats.Application.UnitTests/Ats.Application.UnitTests.csproj
dotnet test tests/Ats.ArchitectureTests/Ats.ArchitectureTests.csproj
dotnet test backend/tests/Ats.Tests/Ats.Tests.csproj
```

### Cobertura de Pruebas:
- **Ats.Domain.UnitTests (11 pruebas):** Validacion de invariantes en entidades de dominio (`Candidate`, `JobPosition`), creacion de `CandidateEmail`, reglas de transicion de estados y eventos de dominio.
- **Ats.Application.UnitTests (14 pruebas):** Pruebas de handlers CQRS (`IngestCandidateCommandHandler`, `ProcessCvAnalysisCommandHandler`), validadores FluentValidation y asignacion de reclutadores.
- **Ats.ArchitectureTests (5 pruebas):** Verificacion estricta de fronteras de Clean Architecture (el dominio no depende de infraestructura, la aplicacion no referencia API, encapsulamiento de contratos).
- **Ats.Tests (39 pruebas):** Deteccion heuristica y sanitizacion contra Prompt Injection, normalizacion de habilidades tecnicas multi-area con catalogo semantico, calculo determinista de calce con el puesto (`JobFitScoringService`) y verificacion de consistencia curricular.

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
- **Fase 8:** Modularidad y Escalabilidad Arquitectonica:
  - Desacoplamiento de motor IA con `MockAiProvider` y `GeminiAiProvider`.
  - Cola en segundo plano desacoplada (`IBackgroundJobQueue` con Channels) y endpoint asincrono `request.Async`.
  - Migraciones formales de EF Core con ejecucion automatica en el arranque (`db.Database.Migrate()`).
  - Abstraccion de almacenamiento en la nube (`IStorageService` con Local y S3).
  - Abstraccion de cache en memoria (`ICacheService`).
  - Modularizacion de componentes frontend (`EvaluatorReviewModal` en pestanas dedicadas).
  - Resolucion de cuello de botella 5N+1 mediante consultas por lote y proteccion de diccionarios.
  - Integracion formal de la suite completa de testing (69 pruebas superadas).

### Fases Planificadas
- **Fase 9:** Notificaciones en tiempo real via WebSockets/SignalR para avisar inmediatamente al reclutador ante nuevas delegaciones de expedientes.
- **Fase 10:** Portal publico de auto-postulacion directa para postulantes con captcha empresarial y limitacion de tasa de peticiones (rate limiting).
- **Fase 11:** Sincronizacion de entrevistas con calendarios corporativos (Google Calendar, Microsoft Outlook / Teams).
- **Fase 12:** Analitica avanzada de pipeline de seleccion y calculo de tiempos de ciclo de contratacion.
