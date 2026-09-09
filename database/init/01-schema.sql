-- =========================================================================================
-- 01-schema.sql: Esquema Canónico Completo DDL de TalentIQ Enterprise ATS
-- Contiene la definición formal de todas las tablas, tipos, índices y claves foráneas.
-- =========================================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Vacantes y Convocatorias Formales
CREATE TABLE IF NOT EXISTS job_positions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(150) NOT NULL,
    department VARCHAR(100) NOT NULL,
    seniority VARCHAR(50) NOT NULL DEFAULT 'Senior',
    min_experience_years INT NOT NULL DEFAULT 3,
    description TEXT,
    requirements TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'Active',
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_job_positions_status ON job_positions(status);

-- 2. Candidatos
CREATE TABLE IF NOT EXISTS candidates (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    "PhoneNumber" VARCHAR(30),
    "EvaluatorDecision" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "EvaluatorNotes" TEXT,
    "EvaluatedAtUtc" TIMESTAMPTZ,
    assignedrecruiterid VARCHAR(100),
    assignedrecruitername VARCHAR(150),
    assignedrecruiteremail VARCHAR(150),
    assignedatutc TIMESTAMPTZ,
    target_role VARCHAR(150),
    job_position_id UUID REFERENCES job_positions(id) ON DELETE SET NULL,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_candidates_job_position_id ON candidates(job_position_id);
CREATE INDEX IF NOT EXISTS idx_candidates_assigned_recruiter ON candidates(assignedrecruiterid);

-- 3. Documentos de CV
CREATE TABLE IF NOT EXISTS cv_documents (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CandidateId" UUID NOT NULL REFERENCES candidates("Id") ON DELETE CASCADE,
    "FileName" VARCHAR(255) NOT NULL,
    "StoragePath" VARCHAR(1000) NOT NULL,
    "FileSizeBytes" BIGINT NOT NULL,
    "ContentType" VARCHAR(100) NOT NULL,
    "UploadedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_cv_documents_candidate_id ON cv_documents("CandidateId");

-- 4. Análisis de CV por IA
CREATE TABLE IF NOT EXISTS candidate_cv_analyses (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CandidateId" UUID NOT NULL REFERENCES candidates("Id") ON DELETE CASCADE,
    "DocumentId" UUID NOT NULL REFERENCES cv_documents("Id") ON DELETE CASCADE,
    "AnalysisJson" JSONB NOT NULL,
    "Status" VARCHAR(30) NOT NULL,
    "ProviderName" VARCHAR(50) NOT NULL,
    "ModelName" VARCHAR(50) NOT NULL,
    "PromptVersion" VARCHAR(20) NOT NULL,
    "ErrorMessage" TEXT,
    "FeedbackJson" JSONB,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_cv_analyses_candidate_id ON candidate_cv_analyses("CandidateId");

-- 5. Evaluaciones Psicométricas y Conductuales Multidimensionales (Agnósticas)
CREATE TABLE IF NOT EXISTS candidate_assessments (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CandidateId" UUID NOT NULL REFERENCES candidates("Id") ON DELETE CASCADE,
    assessment_type VARCHAR(50) NOT NULL DEFAULT 'DISC',
    scores_assessment_type VARCHAR(50),
    primary_style VARCHAR(50) NOT NULL,
    dimensions_json JSONB NOT NULL,
    raw_results_json JSONB,
    completed_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_candidate_assessments_candidate_id ON candidate_assessments("CandidateId");
CREATE INDEX IF NOT EXISTS idx_candidate_assessments_type ON candidate_assessments(assessment_type);

-- 6. Interpretaciones de Evaluaciones por IA (Agnósticas)
CREATE TABLE IF NOT EXISTS candidate_assessment_interpretations (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CandidateId" UUID NOT NULL REFERENCES candidates("Id") ON DELETE CASCADE,
    "AssessmentId" UUID NOT NULL REFERENCES candidate_assessments("Id") ON DELETE CASCADE,
    assessment_type VARCHAR(50) NOT NULL DEFAULT 'DISC',
    interpretation_json JSONB NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    provider_name VARCHAR(50) NOT NULL,
    model_name VARCHAR(50) NOT NULL,
    prompt_version VARCHAR(20) NOT NULL,
    error_message TEXT,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_candidate_assessment_interp_candidate_id ON candidate_assessment_interpretations("CandidateId");
CREATE INDEX IF NOT EXISTS idx_candidate_assessment_interp_assessment_id ON candidate_assessment_interpretations("AssessmentId");

-- 7. Resultados DISC (Compatibilidad Legacy)
CREATE TABLE IF NOT EXISTS candidate_disc_results (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CandidateId" UUID NOT NULL REFERENCES candidates("Id") ON DELETE CASCADE,
    dominance INT NOT NULL CHECK (dominance BETWEEN 0 AND 100),
    influence INT NOT NULL CHECK (influence BETWEEN 0 AND 100),
    steadiness INT NOT NULL CHECK (steadiness BETWEEN 0 AND 100),
    conscientiousness INT NOT NULL CHECK (conscientiousness BETWEEN 0 AND 100),
    primary_style VARCHAR(10) NOT NULL,
    "CompletedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_disc_results_candidate_id ON candidate_disc_results("CandidateId");

-- 8. Interpretaciones DISC (Compatibilidad Legacy)
CREATE TABLE IF NOT EXISTS candidate_disc_interpretations (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CandidateId" UUID NOT NULL REFERENCES candidates("Id") ON DELETE CASCADE,
    "DiscResultId" UUID NOT NULL REFERENCES candidate_disc_results("Id") ON DELETE CASCADE,
    "InterpretationJson" JSONB NOT NULL,
    "Status" VARCHAR(30) NOT NULL,
    "ProviderName" VARCHAR(50) NOT NULL,
    "ModelName" VARCHAR(50) NOT NULL,
    "PromptVersion" VARCHAR(20) NOT NULL,
    "ErrorMessage" TEXT,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_disc_interpretations_candidate_id ON candidate_disc_interpretations("CandidateId");

-- 9. Informes Preentrevista y Guías STAR
CREATE TABLE IF NOT EXISTS candidate_interview_reports (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CandidateId" UUID NOT NULL REFERENCES candidates("Id") ON DELETE CASCADE,
    "CvAnalysisId" UUID REFERENCES candidate_cv_analyses("Id") ON DELETE SET NULL,
    "DiscInterpretationId" UUID REFERENCES candidate_disc_interpretations("Id") ON DELETE SET NULL,
    "ReportContentJson" JSONB NOT NULL,
    "FileUrl" VARCHAR(1000),
    "Status" VARCHAR(30) NOT NULL,
    "Version" INT NOT NULL DEFAULT 1,
    "ProviderName" VARCHAR(50) NOT NULL,
    "ModelName" VARCHAR(50) NOT NULL,
    "PromptVersion" VARCHAR(20) NOT NULL,
    "ErrorMessage" TEXT,
    "GeneratedAtUtc" TIMESTAMPTZ,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_interview_reports_candidate_id ON candidate_interview_reports("CandidateId");

-- 10. Trabajos de Procesamiento en Segundo Plano (Idempotencia y Trazabilidad)
CREATE TABLE IF NOT EXISTS processing_jobs (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CandidateId" UUID NOT NULL REFERENCES candidates("Id") ON DELETE CASCADE,
    "ProcessType" VARCHAR(50) NOT NULL,
    "Status" VARCHAR(30) NOT NULL,
    "Attempts" INT NOT NULL DEFAULT 0,
    "EventId" UUID NOT NULL UNIQUE,
    "CorrelationId" UUID NOT NULL,
    "ErrorCode" VARCHAR(50),
    "ErrorMessage" VARCHAR(2000),
    "StartedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "FinishedAtUtc" TIMESTAMPTZ,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_processing_jobs_event_id ON processing_jobs("EventId");
CREATE INDEX IF NOT EXISTS idx_processing_jobs_correlation_id ON processing_jobs("CorrelationId");
CREATE INDEX IF NOT EXISTS idx_processing_jobs_candidate_id ON processing_jobs("CandidateId");
