# Backend ATS TalentIQ - Arquitectura Limpia y Servicios de Negocio

Modulo de servicios de backend para TalentIQ ATS construido sobre **.NET 10** en C# bajo principios de **Clean Architecture**, segregacion de comandos y consultas (**CQRS**) con MediatR, persistencia relacional con **Entity Framework Core** sobre PostgreSQL 16 y servicios avanzados de evaluacion con **Google Gemini AI**.

> [!NOTE]
> **Aviso de Prototipo Funcional (Backend)**
> La logica de negocio, algoritmos de coincidencia (*match scoring*), validaciones de expediente y generacion de preguntas STAR implementados en esta API constituyen un prototipo demostrativo. No representan una politica de contratacion rigida ni definitiva. Su estructura desacoplada en capas (Dominio, Aplicacion, Infraestructura) permite incorporar facilmente nuevas reglas de seleccion, cambiar modelos de ponderacion o conectar con APIs externas de seleccion corporativa.

---

## 1. Estructura de Proyectos y Capas

La solucion sigue una regla de dependencia estricta unidireccional: las capas externas dependen de las internas, y el dominio se mantiene 100% puro e independiente.

```text
backend/
├── src/
│   ├── Ats.Domain/                 # Entidades puras, Value Objects, Enums, Eventos de Dominio
│   │   ├── Common/                 # BaseEntity, AggregateRoot, IDomainEvent
│   │   ├── Entities/               # Candidate, JobPosition, CvAnalysis, DiscInterpretation, InterviewReport
│   │   ├── Enums/                  # CandidateStatus, EvaluationStatus, JobPositionStatus
│   │   └── ValueObjects/           # Email, Scores, Evidence
│   │
│   ├── Ats.Application/            # Casos de uso de negocio (CQRS), DTOs, Validadores e Interfaces
│   │   ├── Common/                 # Interfaces (IRepository, IAiProvider, IStorageService, ICacheService, etc.)
│   │   ├── DTOs/                   # Records inmutables para transferencia de datos
│   │   └── Features/               # Casos de uso agrupados por agregado:
│   │       ├── Candidates/         # GetCandidates (por lote), GetCandidateById, AssignRecruiter, SubmitDecision
│   │       ├── JobPositions/       # GetJobPositions (con cache), CreateJobPosition, UpdateJobPositionStatus
│   │       ├── Ingestion/          # IngestCandidateCommandHandler & soporte asincrono (IBackgroundJobQueue)
│   │       ├── Scoring/            # JobFitScoringService (calce determinista de puesto y habilidades)
│   │       ├── CvProcessing/       # ProcessCvAnalysisCommandHandler
│   │       ├── Disc/               # ProcessDiscInterpretationCommandHandler
│   │       ├── Documents/          # GetCandidateCvDocumentQueryHandler (streaming binario seguro)
│   │       └── Reports/            # GenerateInterviewReportCommandHandler y GetReportPdfQueryHandler
│   │
│   ├── Ats.Infrastructure/         # Implementacion de adaptadores, persistencia y servicios externos
│   │   ├── Persistence/            # ApplicationDbContext, migraciones formales y repositorios EF Core
│   │   │   ├── Configurations/     # Mapeo Fluent API con PostgreSQL (mapeo jsonb relacional)
│   │   │   ├── Migrations/         # Migraciones formales EF Core
│   │   │   └── Repositories/       # Repositorios optimizados con AsNoTracking() y metodos batch
│   │   └── Services/
│   │       ├── Ai/                 # GeminiAiProvider (resiliencia multi-modelo) y MockAiProvider
│   │       ├── Caching/            # MemoryCacheService (abstraccion ICacheService)
│   │       ├── Jobs/               # ChannelBackgroundJobQueue y QueuedHostedService (BackgroundService)
│   │       ├── Security/           # CvSecuritySanitizer (deteccion regex y escape de tags XML)
│   │       ├── Pdf/                # PdfPigTextExtractor (extraccion segura de texto en memoria)
│   │       ├── Reporting/          # QuestPdfReportGenerator (informe ejecutivo de dos paginas)
│   │       └── Storage/            # LocalStorageService, S3StorageService y StorageOptions
│   │
│   └── Ats.Api/                    # Capa de presentacion, endpoints REST y middlewares
│       ├── Controllers/            # ApiControllerBase, CandidatesController, JobPositionsController, DocumentsController, ReportsController, DiscController, AssessmentsController, IngestionController, WebhooksController
│       ├── Middlewares/            # GlobalExceptionHandlerMiddleware (ProblemDetails RFC 7807)
│       └── Program.cs              # Configuracion de DI, Keycloak JWT, auto-migracion DB y pipeline HTTP
│
├── tests/
│   └── Ats.Tests/                  # Pruebas unitarias de sanitizacion, seguridad, scoring y habilidades
│
└── Dockerfile                      # Compilacion multi-stage (.NET 10 SDK -> ASP.NET Core Runtime)
```

---

## 2. Modelado de Base de Datos y Scripts de Inicializacion

La base de datos PostgreSQL 16 se inicializa de forma declarativa y estructurada a traves de los scripts consolidados en `database/init/`:

1. **`00-create-keycloak-db.sql`:** Crea la base de datos `keycloak_db` aislando completamente los esquemas de identidad y roles de Keycloak.
2. **`01-schema.sql`:** DDL unificado con extensiones UUID, tablas principales (`candidates`, `job_positions`, `cv_documents`, `candidate_cv_analyses`, `candidate_disc_results`, `candidate_disc_interpretations`, `candidate_assessments`, `candidate_assessment_interpretations`, `candidate_interview_reports`, `processing_jobs`), llaves foraneas con cascadas controladas e indices relacionales optimizados.
3. **`02-seed-data.sql`:** DML unificado en codificacion nativa UTF-8 que puebla vacantes formales, candidatos y expedientes completos estructurados segun el esquema JSON de Gemini para pruebas locales inmediatas.

---

## 3. Endpoints de la API REST

### 3.1 Vacantes y Puestos Formales (`/api/v1/positions`)
- **`GET /api/v1/positions`**: Lista todas las vacantes con calculo dinamico de candidatos postulados. (Autorizado: `ats_admin`, `ats_recruiter`).
- **`POST /api/v1/positions`**: Crea una nueva vacante formal con validacion FluentValidation. (Restringido estrictamente a `ats_admin`).
- **`PATCH /api/v1/positions/{id}/status`**: Modifica el estado de una vacante (Activa, Pausada, Cerrada). (Restringido a `ats_admin`).

### 3.2 Postulantes y Expedientes (`/api/v1/candidates`)
- **`GET /api/v1/candidates`**: Consulta candidatos con soporte para busqueda por texto, filtrado por cargo y estado.
- **`GET /api/v1/candidates/{id}`**: Obtiene el expediente completo con analisis de CV, perfil conductual DISC y preguntas STAR.
- **`POST /api/v1/candidates`**: Registra un candidato manualmente con validacion `RegisterCandidateCommandValidator`.
- **`POST /api/v1/candidates/{id}/assign`**: Asigna o reasigna un evaluador responsable al candidato. (Restringido a `ats_admin`).
- **`POST /api/v1/candidates/{id}/decision`**: Registra el dictamen oficial de la entrevista (Aprobado, En Reserva, Descartado) y notas de auditoria.
- **`GET /api/v1/users/recruiters`**: Obtiene el listado de evaluadores disponibles para asignacion de expedientes. (Restringido a `ats_admin`).
- **`PATCH /api/v1/candidates/{id}/cv-feedback`**: Registra observaciones de cotejo curricular sobre habilidades identificadas u omitidas.

### 3.3 Ingesta y Evaluacion en Tiempo Real (`/api/v1/ingestion`)
- **`POST /api/v1/ingestion/evaluate`**: Endpoint multipart/form-data para carga simultanea de CV en PDF, seleccion de vacante y captura de puntajes DISC. Soporta modo sincrono (retorna `200 OK` con expediente completo) o asincrono (`Async=true`, encolando en `IBackgroundJobQueue` y retornando `202 Accepted`).

### 3.4 Reportes Ejecutivos (`/api/v1/reports`)
- **`GET /api/v1/reports/{id}/pdf`**: Generacion y descarga del informe pre-entrevista en PDF a traves del handler CQRS `GetReportPdfQueryHandler` y `QuestPdfReportGenerator`.
- **`GET /api/v1/reports/candidate/{candidateId}/latest`**: Obtiene el informe mas reciente emitido para un postulante.

### 3.5 Evaluacion Conductual y Psicometria (`/api/v1/disc`, `/api/v1/assessments`)
- **`POST /api/v1/disc/results`**: Recepcion y persistencia de dimensiones D, I, S, C y estilo primario.
- **`GET /api/v1/disc/candidate/{candidateId}`**: Consulta del perfil psicometrico interpretado.
- **`POST /api/v1/assessments/results`**: Recepcion generica de dimensiones de evaluacion psicometrica.

### 3.6 Webhooks y Automatizacion (`/api/v1/webhooks`)
- **`POST /api/v1/webhooks/process-cv`**: Ejecuta el procesamiento y analisis curricular de un candidato. Protegido con validacion en tiempo constante (`X-Webhook-Secret` o `X-Api-Key`).
- **`POST /api/v1/webhooks/process-disc`**: Procesa la interpretacion conductual DISC para un candidato y resultado registrado. Protegido con `X-Webhook-Secret` o `X-Api-Key`.
- **`POST /api/v1/webhooks/generate-report`**: Consolida y genera el informe ejecutivo de entrevista. Protegido con `X-Webhook-Secret` o `X-Api-Key`.

### 3.7 Documentos y Salud (`/api/v1/documents`, `/health`)
- **`POST /api/v1/documents/cv`**: Carga de archivo PDF con validacion de tipo MIME y tamano mediante `UploadCvCommandValidator`.
- **`GET /api/v1/documents/cv/{candidateId}`**: Transmision en streaming binario del archivo de curriculum original en PDF con cabecera `Content-Disposition: inline` para previsualizacion directa interactiva en la web. (Autorizado: `ats_admin`, `ats_recruiter`).
- **`GET /health`**: Sondeo de disponibilidad del servicio para balanceadores de carga y healthchecks de Docker.

---

## 4. Mitigaciones de Seguridad Implementadas

1. **Defensa contra Prompt Injection (Cuatro Capas):**
   - Sanitizacion heuristica con `CvSecuritySanitizer`.
   - Confinamiento con `systemInstruction` y delimitacion XML `<untrusted_applicant_cv>`.
   - Verificacion de citas textuales de competencias (*Grounding Check*).
   - Human-in-the-loop: alertas de integridad curricular en frontend y rol utilitario sin decisiones autonomas.

2. **Defensa contra Path Traversal:**
   - [`LocalStorageService.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Infrastructure/Services/Storage/LocalStorageService.cs) normaliza todas las rutas con `Path.GetFullPath()` y verifica que pertenezcan estrictamente al subdirectorio base de almacenamiento antes de realizar lecturas o escrituras.

3. **Prevencion de Timing Attacks:**
   - La validacion de credenciales de webhooks (`X-Webhook-Secret` o `X-Api-Key`) utiliza comparacion en tiempo constante mediante `CryptographicOperations.FixedTimeEquals`.

4. **Manejo Centralizado de Excepciones:**
   - [`GlobalExceptionHandlerMiddleware.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Api/Middlewares/GlobalExceptionHandlerMiddleware.cs) estandariza los errores bajo RFC 7807 (ProblemDetails), evitando la exposicion de trazas de pila internas en entornos de produccion.

5. **Proteccion de Credenciales:**
   - Las claves maestras se leen mediante inyeccion de configuracion de ASP.NET Core desde variables de entorno (`Gemini__ApiKey`, `Webhooks__Secret`, `Ingestion__ApiKey`), sin valores por defecto criticos en codigo fuente.

---

## 5. Pruebas y Validacion

Para ejecutar el conjunto completo de **80 pruebas automatizadas** bajo la solucion unificada:

```bash
# Ejecutar todas las pruebas de la solucion (Domain, Application, Architecture, Tests)
dotnet test Ats.slnx

# Ejecutar proyectos individuales
dotnet test tests/Ats.Domain.UnitTests/Ats.Domain.UnitTests.csproj
dotnet test tests/Ats.Application.UnitTests/Ats.Application.UnitTests.csproj
dotnet test tests/Ats.ArchitectureTests/Ats.ArchitectureTests.csproj
dotnet test backend/tests/Ats.Tests/Ats.Tests.csproj --logger "console;verbosity=detailed"
```

