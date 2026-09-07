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
│   │   ├── Common/                 # Interfaces (IRepository, IAiProvider, IPdfTextExtractor, etc.)
│   │   ├── DTOs/                   # Records inmutables para transferencia de datos
│   │   └── Features/               # Casos de uso agrupados por agregado:
│   │       ├── Candidates/         # GetCandidates, GetCandidateById, AssignRecruiter, SubmitDecision
│   │       ├── JobPositions/       # GetJobPositions, CreateJobPosition, UpdateJobPositionStatus
│   │       ├── Ingestion/          # IngestAndEvaluateCandidateCommand & Pipeline
│   │       ├── CvProcessing/       # ProcessCvCommand
│   │       ├── Disc/               # ProcessDiscCommand
│   │       └── Reports/            # GenerateReportCommand
│   │
│   ├── Ats.Infrastructure/         # Implementacion de adaptadores, persistencia y servicios externos
│   │   ├── Persistence/            # AtsDbContext, migraciones y repositorios EF Core
│   │   │   ├── Configurations/     # Mapeo Fluent API con PostgreSQL
│   │   │   └── Repositories/       # CandidateRepository, JobPositionRepository
│   │   └── Services/
│   │       ├── Ai/                 # GeminiAiService con systemInstruction y defensas anti-prompt injection
│   │       ├── Security/           # CvSecuritySanitizer (deteccion regex y escape de tags XML)
│   │       ├── Pdf/                # PdfPigTextExtractor (extraccion segura de texto en memoria)
│   │       ├── Reporting/          # QuestPdfReportGenerator (informe ejecutivo de dos paginas)
│   │       └── Storage/            # StorageService (gestion de archivos con defensa contra Path Traversal)
│   │
│   └── Ats.Api/                    # Capa de presentacion, endpoints REST y middlewares
│       ├── Controllers/            # CandidatesController, JobPositionsController, IngestionController, WebhooksController
│       ├── Middlewares/            # GlobalExceptionHandlerMiddleware (ProblemDetails RFC 7807)
│       └── Program.cs              # Configuracion de DI, autenticacion Keycloak JWT y pipeline HTTP
│
├── tests/
│   └── Ats.Tests/                  # Pruebas automatizadas xUnit (sanitizacion, seguridad y casos de uso)
│
└── Dockerfile                      # Compilacion multi-stage (.NET 10 SDK -> ASP.NET Core Runtime)
```

---

## 2. Modelado de Base de Datos y Migraciones

La base de datos se inicializa y actualiza secuencialmente a traves de los scripts situados en `database/init/`:

1. **`01-init-schema.sql`:** Creacion de tablas base (`candidates`, `cv_analyses`, `disc_interpretations`, `interview_reports`, `processing_jobs`).
2. **`02-seed-realistic-data.sql`:** Insercion de postulantes representativos para pruebas locales.
3. **`03-add-foreign-keys.sql`:** Definicion de restricciones de integridad referencial y llaves foraneas.
4. **`04-add-recruiter-assignment.sql`:** Soporte para delegacion de evaluadores (`assigned_recruiter_id`, `assigned_recruiter_name`).
5. **`05-add-interview-decision.sql`:** Registro del veredicto oficial (`interview_decision`, `interview_notes`, `evaluated_at`).
6. **`06-create-job-positions.sql`:** Creacion de la tabla `job_positions` y adicion de la llave foranea `job_position_id` en `candidates`.

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
- **`POST /api/v1/ingestion/evaluate`**: Endpoint multipart/form-data para carga simultanea de CV en PDF, seleccion de vacante y captura de puntajes DISC. Ejecuta el pipeline completo de Gemini AI y retorna el expediente listo.

### 3.4 Webhooks y Automatizacion (`/api/v1/webhooks`)
- **`POST /api/v1/webhooks/cv-processed`**: Recibe resultados de procesamiento asincrono de CV desde n8n. Protegido con firma HMAC SHA-256 en encabezado `X-ATS-Signature`.
- **`POST /api/v1/webhooks/disc-processed`**: Recibe interpretacion DISC asincrona. Validacion de firma con `CryptographicOperations.FixedTimeEquals`.

### 3.5 Documentos y Salud (`/api/v1/documents`, `/health`)
- **`GET /api/v1/documents/{id}/download`**: Descarga segura del archivo PDF original o reporte generado.
- **`GET /health`**: Sondeo de disponibilidad del servicio para balanceadores de carga y healthchecks de Docker.

---

## 4. Mitigaciones de Seguridad Implementadas

1. **Defensa contra Prompt Injection (Cuatro Capas):**
   - Sanitizacion heuristica con `CvSecuritySanitizer`.
   - Confinamiento con `systemInstruction` y delimitacion XML `<untrusted_applicant_cv>`.
   - Verificacion de citas textuales de competencias (*Grounding Check*).
   - Human-in-the-loop: alertas de integridad curricular en frontend.

2. **Defensa contra Path Traversal:**
   - [`StorageService.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Infrastructure/Services/Storage/StorageService.cs) normaliza todas las rutas con `Path.GetFullPath()` y verifica que pertenezcan estrictamente al subdirectorio base de almacenamiento antes de realizar lecturas o escrituras.

3. **Prevencion de Timing Attacks:**
   - La validacion de firmas HMAC de webhooks utiliza comparacion en tiempo constante mediante `CryptographicOperations.FixedTimeEquals`.

4. **Manejo Centralizado de Excepciones:**
   - [`GlobalExceptionHandlerMiddleware.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Api/Middlewares/GlobalExceptionHandlerMiddleware.cs) estandariza los errores bajo RFC 7807 (ProblemDetails), evitando la exposicion de trazas de pila internas en entornos de produccion.

5. **Proteccion de Credenciales:**
   - Las claves maestras se leen mediante inyeccion de configuracion de ASP.NET Core desde variables de entorno (`Gemini__ApiKey`, `Webhooks__Secret`, `Ingestion__ApiKey`).

---

## 5. Pruebas y Validacion

Para ejecutar el conjunto de pruebas automatizadas:

```bash
# Ejecutar todas las pruebas unitarias
dotnet test backend/tests/Ats.Tests/Ats.Tests.csproj

# Ejecutar con reporte detallado
dotnet test backend/tests/Ats.Tests/Ats.Tests.csproj --logger "console;verbosity=detailed"
```

