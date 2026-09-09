-- 07-create-generic-assessments.sql: Abstracción a Evaluaciones Genéricas Multidimensionales

CREATE TABLE IF NOT EXISTS candidate_assessments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    assessment_type VARCHAR(50) NOT NULL DEFAULT 'DISC',
    primary_style VARCHAR(50) NOT NULL,
    dimensions_json JSONB NOT NULL,
    raw_results_json JSONB NULL,
    completed_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_candidate_assessments_candidate_id ON candidate_assessments(candidate_id);
CREATE INDEX IF NOT EXISTS idx_candidate_assessments_type ON candidate_assessments(assessment_type);

CREATE TABLE IF NOT EXISTS candidate_assessment_interpretations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    assessment_id UUID NOT NULL REFERENCES candidate_assessments(id) ON DELETE CASCADE,
    assessment_type VARCHAR(50) NOT NULL DEFAULT 'DISC',
    interpretation_json JSONB NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    provider_name VARCHAR(50) NOT NULL,
    model_name VARCHAR(50) NOT NULL,
    prompt_version VARCHAR(20) NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at_utc TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS idx_candidate_assessment_interp_candidate_id ON candidate_assessment_interpretations(candidate_id);
CREATE INDEX IF NOT EXISTS idx_candidate_assessment_interp_assessment_id ON candidate_assessment_interpretations(assessment_id);

-- Migración de datos históricos de DISC hacia la tabla genérica
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'candidate_disc_results') THEN
        INSERT INTO candidate_assessments (id, candidate_id, assessment_type, primary_style, dimensions_json, completed_at_utc)
        SELECT 
            d.id,
            d.candidate_id,
            'DISC',
            COALESCE(d.primary_style, 'D/C'),
            jsonb_build_object(
                'Dominance', d.dominance,
                'Influence', d.influence,
                'Steadiness', d.steadiness,
                'Conscientiousness', d.conscientiousness
            ),
            d.completed_at_utc
        FROM candidate_disc_results d
        ON CONFLICT (id) DO NOTHING;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'candidate_disc_interpretations') THEN
        INSERT INTO candidate_assessment_interpretations (
            id, candidate_id, assessment_id, assessment_type, interpretation_json, 
            status, provider_name, model_name, prompt_version, created_at_utc, processed_at_utc
        )
        SELECT 
            i.id,
            i.candidate_id,
            i.disc_result_id,
            'DISC',
            i.interpretation_json,
            i.status,
            i.provider_name,
            i.model_name,
            i.prompt_version,
            i.created_at_utc,
            i.processed_at_utc
        FROM candidate_disc_interpretations i
        JOIN candidate_assessments ca ON ca.id = i.disc_result_id
        ON CONFLICT (id) DO NOTHING;
    END IF;
END $$;
