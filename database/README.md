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
| **`01-init.sql`** | Esquema base del ATS | Crea las tablas nucleares: `candidates`, `cv_analyses`, `disc_interpretations`, `interview_reports`, `processing_jobs`. |
| **`02-seed-realistic-data.sql`** | Semilla de datos | Carga perfiles representativos de postulantes con datos curriculares, DISC y preguntas STAR para pruebas. |
| **`03-add-foreign-keys.sql`** | Integridad referencial | Establece llaves foraneas explícitas e indices relacionales con reglas `ON DELETE CASCADE`. |
| **`04-add-evaluator-columns.sql`** | Dictamen de evaluacion | Incorpora columnas para resolucion del comite: `interview_decision`, `interview_notes`, `evaluated_at`. |
| **`05-add-assignment-columns.sql`** | Delegacion de reclutadores | Incorpora campos de asignacion: `assigned_recruiter_id`, `assigned_recruiter_name`. |
| **`06-create-job-positions.sql`** | Catalogo de vacantes | Crea la tabla `job_positions` y anade la columna `job_position_id` indexada en `candidates` con regla `ON DELETE SET NULL`. |

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
  │ id (PK, UUID)                                             │
  │ job_position_id (FK -> job_positions.id, nullable)       │
  │ first_name, last_name, email, phone, target_role          │
  │ status, current_step, cv_file_path                        │
  │ assigned_recruiter_id, assigned_recruiter_name            │
  │ interview_decision, interview_notes, evaluated_at         │
  │ created_at_utc, updated_at_utc                            │
  └────────┬────────────────────────┬─────────────────────────┘
           │ 1                      │ 1
           │ 0..1                   │ 0..1
           ▼                        ▼
  ┌─────────────────────────┐   ┌─────────────────────────┐
  │       cv_analyses       │   │   disc_interpretations  │
  ├─────────────────────────┤   ├─────────────────────────┤
  │ id (PK, UUID)           │   │ id (PK, UUID)           │
  │ candidate_id (FK)       │   │ candidate_id (FK)       │
  │ executive_summary       │   │ dominance_score         │
  │ extracted_skills (JSONB)│   │ influence_score         │
  │ work_experiences (JSONB)│   │ steadiness_score        │
  │ education (JSONB)       │   │ compliance_score        │
  │ security_flags (JSONB)  │   │ primary_style           │
  │ created_at_utc          │   │ workplace_descriptors   │
  └────────┬────────────────┘   └────────┬────────────────┘
           │                             │
           └──────────────┬──────────────┘
                          │ 1..1
                          ▼
            ┌───────────────────────────┐
            │     interview_reports     │
            ├───────────────────────────┤
            │ id (PK, UUID)             │
            │ candidate_id (FK)         │
            │ cv_analysis_id (FK)       │
            │ disc_interpretation_id(FK)│
            │ executive_summary         │
            │ behavioral_profile        │
            │ key_strengths             │
            │ red_flags                 │
            │ star_questions (JSONB)    │
            │ report_file_path          │
            │ created_at_utc            │
            └───────────────────────────┘
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

