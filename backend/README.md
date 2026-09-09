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
│   │   │   ├── Configurations/     # Mapeo Fluent API con PostgreSQL
│   │   │   ├── Migrations/         # Migraciones formales EF Core (InitialCreate)
│   │   │   ├── Configurations/     # Mapeo Fluent API con PostgreSQL (incluyendo jsonb para FeedbackJson)
│   │   │   ├── Migrations/         # Migraciones formales EF Core
│   │   │   └── Repositories/       # CandidateRepository, JobPositionRepository, etc. (con metodos por lote)
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
│       ├── Controllers/            # CandidatesController, JobPositionsController, IngestionController, ReportsController
│       ├── Controllers/            # CandidatesController, PositionsController, IngestionController, ReportsController, DocumentsController
│       ├── Middlewares/            # GlobalExceptionHandlerMiddleware (ProblemDetails RFC 7807)
│       └── Program.cs              # Configuracion de DI, Keycloak JWT, auto-migracion DB y pipeline HTTP
│
├── tests/
│   └── Ats.Tests/                  # Pruebas unitarias de sanitizacion, seguridad, scoring y habilidades
│
└── Dockerfile                      # Compilacion multi-stage (.NET 10 SDK -> ASP.NET Core Runtime)
```

---

## 2. Modelado de Base de Datos y Migraciones
## 2. Modelado de Base de Datos y Scripts de Inicializacion

La base de datos se inicializa y actualiza secuencialmente a traves de los scripts situados en `database/init/`:
La base de datos PostgreSQL 16 se inicializa de forma declarativa y estructurada a traves de los scripts consolidados en `database/init/`:

1. **`01-init-schema.sql`:** Creacion de tablas base (`candidates`, `cv_analyses`, `disc_interpretations`, `interview_reports`, `processing_jobs`).
2. **`02-seed-realistic-data.sql`:** Insercion de postulantes representativos para pruebas locales.
3. **`03-add-foreign-keys.sql`:** Definicion de restricciones de integridad referencial y llaves foraneas.
4. **`04-add-recruiter-assignment.sql`:** Soporte para delegacion de evaluadores (`assigned_recruiter_id`, `assigned_recruiter_name`).
5. **`05-add-interview-decision.sql`:** Registro del veredicto oficial (`interview_decision`, `interview_notes`, `evaluated_at`).
6. **`06-create-job-positions.sql`:** Creacion de la tabla `job_positions` y adicion de la llave foranea `job_position_id` en `candidates`.
1. **`00-create-keycloak-db.sql`:** Crea la base de datos `keycloak_db` aislando completamente los esquemas de identidad y roles de Keycloak.
2. **`01-schema.sql`:** DDL unificado con extensiones UUID, tablas principales (`candidates`, `job_positions`, `cv_documents`, `candidate_cv_analyses`, `candidate_disc_results`, `candidate_disc_interpretations`, `candidate_assessments`, `candidate_assessment_interpretations`, `candidate_interview_reports`, `processing_jobs`), llaves foraneas con cascadas controladas e indices relacionales optimizados.
3. **`02-seed-data.sql`:** DML unificado en codificacion nativa UTF-8 que puebla vacantes formales, candidatos y expedientes completos estructurados segun el esquema JSON de Gemini para pruebas locales inmediatas.

---

## 3. Endpoints de la API REST

### 3.1 Vacantes y Puestos Formales (`/api/v1/positions`)
- **`GET /api/v1/positions`**: Lista todas las vacantes con calculo dinamico de candidatos postulados. (Autorizado: `ats_admin`, `ats_recruiter`).
- **`POST /api/v1/positions`**: Crea una nueva vacante formal. (Restringido estrictamente a `ats_admin`).
- **`PATCH /api/v1/positions/{id}/status`**: Modifica el estado de una vacante (Activa, Pausada, Cerrada). (Restringido a `ats_admin`).

### 3.2 Postulantes y Expedientes (`/api/v1/candidates`)
- **`GET /api/v1/candidates`**: Consulta candidatos con soporte para busqueda por texto, filtrado por cargo y estado.
- **`GET /api/v1/candidates/{id}`**: Obtiene el expediente completo con analisis de CV, perfil conductual DISC y preguntas STAR.
- **`POST /api/v1/candidates/{id}/assign`**: Asigna o reasigna un evaluador responsable al candidato. (Restringido a `ats_admin`).
- **`POST /api/v1/candidates/{id}/decision`**: Registra el dictamen oficial de la entrevista (Aprobado, En Reserva, Descartado) y notas de auditoria.

### 3.3 Ingesta y Evaluacion en Tiempo Real (`/api/v1/ingestion`)
- **`POST /api/v1/ingestion/evaluate`**: Endpoint multipart/form-data para carga simultanea de CV en PDF, seleccion de vacante y captura de puntajes DISC. Soporta modo sincrono (retorna `200 OK` con expediente completo) o asincrono (`Async=true`, encolando en `IBackgroundJobQueue` y retornando `202 Accepted`).

### 3.4 Reportes Ejecutivos (`/api/v1/reports`)
- **`GET /api/v1/reports/{id}/pdf`**: Generacion y descarga del informe pre-entrevista en PDF a traves del handler CQRS `GetReportPdfQueryHandler` y `QuestPdfReportGenerator`.

### 3.5 Webhooks y Automatizacion (`/api/v1/webhooks`)
- **`POST /api/v1/webhooks/cv-processed`**: Recibe resultados de procesamiento asincrono de CV desde n8n. Protegido con firma HMAC SHA-256 en encabezado `X-ATS-Signature`.
- **`POST /api/v1/webhooks/disc-processed`**: Recibe interpretacion DISC asincrona. Validacion de firma con `CryptographicOperations.FixedTimeEquals`.

### 3.6 Documentos y Salud (`/api/v1/documents`, `/health`)
- **`GET /api/v1/documents/{id}/download`**: Descarga segura del archivo PDF original o reporte generado.
- **`GET /api/v1/documents/cv/{candidateId}`**: Transmision en streaming binario del archivo de curriculum original en PDF con cabecera `Content-Disposition: inline` para previsualizacion directa interactiva en la web. (Autorizado: `ats_admin`, `ats_recruiter`).
- **`GET /api/v1/documents/{id}/download`**: Descarga directa y segura del archivo binario.
- **`GET /health`**: Sondeo de disponibilidad del servicio para balanceadores de carga y healthchecks de Docker.

---

## 4. Mitigaciones de Seguridad Implementadas

1. **Defensa contra Prompt Injection (Cuatro Capas):**
   - Sanitizacion heuristica con `CvSecuritySanitizer`.
   - Confinamiento con `systemInstruction` y delimitacion XML `<untrusted_applicant_cv>`.
   - Verificacion de citas textuales de competencias (*Grounding Check*).
   - Human-in-the-loop: alertas de integridad curricular en frontend.
   - Human-in-the-loop: alertas de integridad curricular en frontend y rol utilitario sin decisiones autonomas.

2. **Defensa contra Path Traversal:**
   - [`LocalStorageService.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Infrastructure/Services/Storage/LocalStorageService.cs) normaliza todas las rutas con `Path.GetFullPath()` y verifica que pertenezcan estrictamente al subdirectorio base de almacenamiento antes de realizar lecturas o escrituras.

3. **Prevencion de Timing Attacks:**
   - La validacion de firmas HMAC de webhooks utiliza comparacion en tiempo constante mediante `CryptographicOperations.FixedTimeEquals`.

4. **Manejo Centralizado de Excepciones:**
   - [`GlobalExceptionHandlerMiddleware.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Api/Middlewares/GlobalExceptionHandlerMiddleware.cs) estandariza los errores bajo RFC 7807 (ProblemDetails), evitando la exposicion de trazas de pila internas en entornos de produccion.

5. **Proteccion de Credenciales:**
   - Las claves maestras se leen mediante inyeccion de configuracion de ASP.NET Core desde variables de entorno (`Gemini__ApiKey`, `Webhooks__Secret`, `Ingestion__ApiKey`), sin valores por defecto criticos en codigo fuente.

---

## 5. Pruebas y Validacion

Para ejecutar el conjunto completo de 69 pruebas automatizadas bajo la solucion unificada:
Para ejecutar el conjunto completo de **80 pruebas automatizadas** bajo la solucion unificada:

```bash
# Ejecutar todas las pruebas de la solucion (Domain, Application, Architecture, Tests)
dotnet test Ats.slnx

# Ejecutar proyecto especifico con reporte detallado
# Ejecutar proyectos individuales
dotnet test tests/Ats.Domain.UnitTests/Ats.Domain.UnitTests.csproj
dotnet test tests/Ats.Application.UnitTests/Ats.Application.UnitTests.csproj
dotnet test tests/Ats.ArchitectureTests/Ats.ArchitectureTests.csproj
dotnet test backend/tests/Ats.Tests/Ats.Tests.csproj --logger "console;verbosity=detailed"
```

