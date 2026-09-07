-- 06-create-job-positions.sql: Módulo de Vacantes y Convocatorias Formales

CREATE TABLE IF NOT EXISTS job_positions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
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

-- Relacionar candidatos con vacante
ALTER TABLE candidates
ADD COLUMN IF NOT EXISTS target_role VARCHAR(150) NULL,
ADD COLUMN IF NOT EXISTS job_position_id UUID NULL REFERENCES job_positions(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_candidates_job_position_id ON candidates(job_position_id);

-- Sembrar vacantes iniciales activas
INSERT INTO job_positions (id, title, department, seniority, min_experience_years, description, requirements, status)
VALUES
    ('11111111-2222-3333-4444-555555555501', 'Senior Backend Engineer', 'Tecnología & Arquitectura', 'Senior', 5, 
     'Responsable de diseñar, desarrollar y optimizar microservicios de alto rendimiento y APIs transaccionales.', 
     'C#, .NET Core, PostgreSQL, Docker, Kubernetes, Arquitectura Limpia, Microservicios.', 'Active'),
    ('11111111-2222-3333-4444-555555555502', 'Lead Cloud Architect', 'Infraestructura Cloud', 'Lead', 7, 
     'Liderar la estrategia de infraestructura multicloud, observabilidad, seguridad y alta disponibilidad.', 
     'Azure, AWS, Terraform, Kubernetes, Seguridad Cloud, CI/CD, Monitoreo y Observabilidad.', 'Active'),
    ('11111111-2222-3333-4444-555555555503', 'Data Engineer', 'Datos & Analítica', 'Senior', 4, 
     'Construcción y orquestación de pipelines de datos masivos, modelos analíticos y gobernanza de datos.', 
     'Python, SQL, PostgreSQL, BigQuery, Airflow, Spark, Modelado de Datos.', 'Active'),
    ('11111111-2222-3333-4444-555555555504', 'Fullstack Developer', 'Desarrollo de Software', 'Semi-Senior', 3, 
     'Desarrollo integral de módulos web modernos, integración con APIs RESTful y experiencia de usuario fluida.', 
     'TypeScript, React, Node.js / .NET, Tailwind CSS, PostgreSQL, Git.', 'Active')
ON CONFLICT (id) DO NOTHING;

-- Actualizar los candidatos existentes para que tengan su target_role y job_position_id correspondientes
UPDATE candidates
SET target_role = 'Senior Backend Engineer',
    job_position_id = '11111111-2222-3333-4444-555555555501'
WHERE email LIKE '%mateo%' OR email LIKE '%carlos%' OR target_role IS NULL;

UPDATE candidates
SET target_role = 'Lead Cloud Architect',
    job_position_id = '11111111-2222-3333-4444-555555555502'
WHERE email LIKE '%david%' OR email LIKE '%rangel%';

UPDATE candidates
SET target_role = 'Data Engineer',
    job_position_id = '11111111-2222-3333-4444-555555555503'
WHERE email LIKE '%valeria%' OR email LIKE '%rios%';

UPDATE candidates
SET target_role = 'Fullstack Developer',
    job_position_id = '11111111-2222-3333-4444-555555555504'
WHERE email LIKE '%sofia%' OR email LIKE '%valenzuela%';