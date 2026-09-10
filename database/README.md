# Base de Datos Relacional ATS TalentIQ (PostgreSQL 16)

Documentacion del modelo relacional de datos, esquemas, restricciones de integridad referencial y scripts de migracion e inicializacion automatica de PostgreSQL para TalentIQ ATS.

> [!NOTE]
> **Aviso de Modelo de Datos - Prototipo**
> El modelo entidad-relación y los datos de prueba provistos en estos scripts fueron disenados como prototipo demostrativo. Las tablas, columnas y tipos de datos pueden modificarse, normalizarse o extenderse (por ejemplo, para agregar etapas de entrevistas en panel, evaluaciones psicométricas adicionales o integraciones con nómina/HRIS) sin alterar la arquitectura central del sistema.

---

## 1. Scripts de Inicializacion Secuencial (`database/init/`)

Cuando el contenedor `ats_postgres` arranca por primera vez, ejecuta automaticamente en orden alfabetico los scripts contenidos en `/docker-entrypoint-initdb.d/`:

| Script | Proposito | Descripcion |
| :--- | :--- | :--- |
| **`00-create-keycloak-db.sql`** | Base de datos para IAM | Ejecuta `CREATE DATABASE keycloak_db;` para aislar el esquema de usuarios y roles de Keycloak. |
| **`01-schema.sql`** | Esquema DDL Consolidado del ATS | Define extensiones UUID, tablas nucleares (`candidates`, `job_positions`, `cv_documents`, `candidate_cv_analyses`, `candidate_disc_results`, `candidate_disc_interpretations`, `candidate_assessments`, `candidate_assessment_interpretations`, `candidate_interview_reports`, `processing_jobs`), llaves foráneas indexadas y restricciones de integridad referencial. |
| **`02-seed-data.sql`** | Datos Semilla DML (Nativo UTF-8) | Carga perfiles representativos completos, vacantes formales activas, evaluaciones curriculares estructuradas en JSONB, mediciones conductuales y guías de preguntas STAR para pruebas locales inmediatas. |

---

## 2. Diagrama Entidad-Relacion (ERD)

```text
  ┌─────────────────────────┐
  │      job_positions      │
  ├─────────────────────────┤
  │ id (PK, UUID)           │
  │ title (VARCHAR 150)     │
  │ department (VARCHAR 100)│
  │ seniority (VARCHAR 50)  │
  │ min_experience_years    │
  │ description (TEXT)      │
  │ requirements (TEXT)     │
  │ status (VARCHAR 30)     │
  │ created_at_utc          │
  │ updated_at_utc          │
  └────────────┬────────────┘
               │ 1
               │
               │ 0..N (ON DELETE SET NULL)
               ▼
  ┌───────────────────────────────────────────────────────────┐
  │                        candidates                         │
  ├───────────────────────────────────────────────────────────┤
  │ Id (PK, UUID)                                             │
  │ job_position_id (FK -> job_positions.id, nullable)       │
  │ FirstName, LastName, email, PhoneNumber, target_role      │
  │ assignedrecruiterid, assignedrecruitername                │
  │ assignedrecruiteremail, assignedatutc                     │
  │ EvaluatorDecision, EvaluatorNotes, EvaluatedAtUtc         │
  │ CreatedAtUtc, UpdatedAtUtc                                │
  └────────┬──────────────┬──────────────┬──────────────┬─────┘
           │ 1            │ 1            │ 1            │ 1
           │ 0..N         │ 0..1         │ 0..1         │ 0..1
           ▼              ▼              ▼              ▼
  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌───────────────────────────┐
  │  cv_documents   │ │candidate_cv_    │ │candidate_disc_  │ │candidate_interview_     │
  │                 │ │analyses         │ │results          │ │reports                   │
  ├─────────────────┤ ├─────────────────┤ ├─────────────────┤ ├───────────────────────────┤
  │ Id (PK, UUID)   │ │ Id (PK, UUID)   │ │ Id (PK, UUID)   │ │ Id (PK, UUID)             │
  │ CandidateId (FK)│ │ CandidateId (FK)│ │ CandidateId (FK)│ │ CandidateId (FK)          │
  │ FileName        │ │ DocumentId (FK) │ │ DominanceScore  │ │ ReportJson (JSONB)        │
  │ StoragePath     │ │ AnalysisJson    │ │ InfluenceScore  │ │ ReportPdfPath             │
  │ FileSizeBytes   │ │ Status          │ │ SteadinessScore │ │ CreatedAtUtc              │
  │ ContentType     │ │ ModelProvider   │ │ ComplianceScore │ └───────────────────────────┘
  │ UploadedAtUtc   │ │ ModelVersion    │ │ PrimaryStyle    │
  └─────────────────┘ │ CreatedAtUtc    │ │ SecondaryStyle  │
                      └─────────────────┘ └────────┬────────┘
                                                   │ 1
                                                   │ 0..1
                                                   ▼
                                          ┌─────────────────────────────┐
                                          │candidate_disc_              │
                                          │interpretations              │
                                          ├─────────────────────────────┤
                                          │ Id (PK, UUID)               │
                                          │ CandidateId (FK)            │
                                          │ DiscResultId (FK)           │
                                          │ WorkplaceDescriptors        │
                                          │ Strengths (TEXT[])          │
                                          │ LeadershipStyle             │
                                          │ CreatedAtUtc                │
                                          └─────────────────────────────┘
```

---

## 3. Acceso y Comandos de Administracion

Para acceder directamente a la consola interactiva psql dentro del contenedor:

```bash
# Conectarse a la base de datos ats_db
docker compose exec postgres psql -U postgres -d ats_db

# Consultar las tablas existentes
\dt

# Ver el esquema detallado de una tabla
\d+ candidates
\d+ job_positions

# Verificar el estado de la base de datos de Keycloak
docker compose exec postgres psql -U postgres -d keycloak_db -c "\dt"
```

---

## 4. Migraciones de Entity Framework Core

Adicionalmente a los scripts SQL de inicializacion rapida para Docker, el backend cuenta con migraciones formales de EF Core en `backend/src/Ats.Infrastructure/Persistence/Migrations/`:

- **Migracion Inicial:** `InitialCreate` (creacion y parametrizacion completa del modelo relacional en C#).
- **Auto-migracion en arranque:** El metodo `Program.cs` ejecuta `db.Database.Migrate()` en el inicio del servicio, asegurando que cualquier cambio en las entidades de dominio se aplique automaticamente al arrancar el contenedor o la aplicacion en local.
- **Comandos de gestion:**
  ```bash
  # Agregar una nueva migracion
  dotnet ef migrations add <NombreMigracion> --project backend/src/Ats.Infrastructure --startup-project backend/src/Ats.Api

  # Aplicar migraciones manualmente
  dotnet ef database update --project backend/src/Ats.Infrastructure --startup-project backend/src/Ats.Api
  ```

