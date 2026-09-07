-- Esquema inicial de base de datos ATS - PostgreSQL

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Tabla de candidatos
CREATE TABLE IF NOT EXISTS candidates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    phone_number VARCHAR(30),
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ
);

-- 2. Tabla de documentos de CV
CREATE TABLE IF NOT EXISTS cv_documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    file_name VARCHAR(255) NOT NULL,
    storage_path VARCHAR(1000) NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    uploaded_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_cv_documents_candidate_id ON cv_documents(candidate_id);

-- 3. Tabla de análisis de CV
CREATE TABLE IF NOT EXISTS candidate_cv_analyses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    document_id UUID NOT NULL REFERENCES cv_documents(id) ON DELETE CASCADE,
    analysis_json JSONB NOT NULL,
    status VARCHAR(30) NOT NULL,
    provider_name VARCHAR(50) NOT NULL,
    model_name VARCHAR(50) NOT NULL,
    prompt_version VARCHAR(20) NOT NULL,
    error_message TEXT,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_cv_analyses_candidate_id ON candidate_cv_analyses(candidate_id);

-- 4. Tabla de resultados DISC
CREATE TABLE IF NOT EXISTS candidate_disc_results (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    dominance INT NOT NULL CHECK (dominance BETWEEN 0 AND 100),
    influence INT NOT NULL CHECK (influence BETWEEN 0 AND 100),
    steadiness INT NOT NULL CHECK (steadiness BETWEEN 0 AND 100),
    conscientiousness INT NOT NULL CHECK (conscientiousness BETWEEN 0 AND 100),
    primary_style VARCHAR(10) NOT NULL,
    completed_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_disc_results_candidate_id ON candidate_disc_results(candidate_id);

-- 5. Tabla de interpretaciones DISC
CREATE TABLE IF NOT EXISTS candidate_disc_interpretations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    disc_result_id UUID NOT NULL REFERENCES candidate_disc_results(id) ON DELETE CASCADE,
    interpretation_json JSONB NOT NULL,
    status VARCHAR(30) NOT NULL,
    provider_name VARCHAR(50) NOT NULL,
    model_name VARCHAR(50) NOT NULL,
    prompt_version VARCHAR(20) NOT NULL,
    error_message TEXT,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_disc_interpretations_candidate_id ON candidate_disc_interpretations(candidate_id);

-- 6. Tabla de informes preentrevista
CREATE TABLE IF NOT EXISTS candidate_interview_reports (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    cv_analysis_id UUID REFERENCES candidate_cv_analyses(id),
    disc_interpretation_id UUID REFERENCES candidate_disc_interpretations(id),
    report_content_json JSONB NOT NULL,
    file_url VARCHAR(1000),
    status VARCHAR(30) NOT NULL,
    version INT NOT NULL DEFAULT 1,
    provider_name VARCHAR(50) NOT NULL,
    model_name VARCHAR(50) NOT NULL,
    prompt_version VARCHAR(20) NOT NULL,
    error_message TEXT,
    generated_at_utc TIMESTAMPTZ,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_interview_reports_candidate_id ON candidate_interview_reports(candidate_id);

-- 7. Tabla de trabajos de procesamiento (Idempotencia y trazabilidad)
CREATE TABLE IF NOT EXISTS processing_jobs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    process_type VARCHAR(50) NOT NULL,
    status VARCHAR(30) NOT NULL,
    attempts INT NOT NULL DEFAULT 0,
    event_id UUID NOT NULL UNIQUE,
    correlation_id UUID NOT NULL,
    error_code VARCHAR(50),
    error_message VARCHAR(2000),
    started_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    finished_at_utc TIMESTAMPTZ,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_processing_jobs_event_id ON processing_jobs(event_id);
CREATE INDEX IF NOT EXISTS idx_processing_jobs_correlation_id ON processing_jobs(correlation_id);

