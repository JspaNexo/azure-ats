-- =========================================================================================
-- 02-seed-data.sql: Datos Semilla Iniciales y Realistas (DML) de TalentIQ Enterprise ATS
-- Carga vacantes activas, candidatos completos con perfiles, CVs, evaluaciones y reportes.
-- =========================================================================================

-- Limpieza preventiva de datos previos
TRUNCATE TABLE 
    processing_jobs, 
    candidate_interview_reports, 
    candidate_assessment_interpretations, 
    candidate_assessments, 
    candidate_disc_interpretations, 
    candidate_disc_results, 
    candidate_cv_analyses, 
    cv_documents, 
    candidates, 
    job_positions 
CASCADE;

-- =========================================================================================
-- VACANTES ACTIVAS (JOB POSITIONS)
-- =========================================================================================
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

-- =========================================================================================
-- CANDIDATO 1: Sofía Valenzuela Navarro - Tech Lead / Software Architect (.NET & Cloud)
-- =========================================================================================
INSERT INTO candidates (
    "Id", "FirstName", "LastName", email, "PhoneNumber",
    "EvaluatorDecision", "EvaluatorNotes", "EvaluatedAtUtc",
    assignedrecruiterid, assignedrecruitername, assignedrecruiteremail, assignedatutc,
    target_role, job_position_id, "CreatedAtUtc"
) VALUES (
    '11111111-1111-1111-1111-111111111111',
    'Sofía',
    'Valenzuela Navarro',
    'sofia.valenzuela@techlead.dev',
    '+1 (809) 555-4321',
    'Approved',
    'Perfil de clase mundial con excelente visión técnica y liderazgo.',
    NOW() - INTERVAL '1 day',
    'laura.sanchez',
    'Laura Sánchez',
    'laura.sanchez@empresa.com',
    NOW() - INTERVAL '2 days',
    'Lead Cloud Architect',
    '11111111-2222-3333-4444-555555555502',
    NOW() - INTERVAL '2 days'

);

INSERT INTO cv_documents ("Id", "CandidateId", "FileName", "StoragePath", "FileSizeBytes", "ContentType", "UploadedAtUtc")
VALUES (
    '11111111-1111-1111-1111-222222222222',
    '11111111-1111-1111-1111-111111111111',
    'CV_Sofia_Valenzuela_TechLead.pdf',
    '/app/Storage/CV_Sofia_Valenzuela_TechLead.pdf',
    184500,
    'application/pdf',
    NOW() - INTERVAL '2 days'
);

INSERT INTO candidate_cv_analyses ("Id", "CandidateId", "DocumentId", "AnalysisJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '11111111-1111-1111-1111-333333333333',
    '11111111-1111-1111-1111-111111111111',
    '11111111-1111-1111-1111-222222222222',
    $${
        "professionalSummary": "Arquitecta de Software y Tech Lead con 9 años de experiencia liderando la transformación de monolitos hacia microservicios en .NET 9/10, Kubernetes, Event-Driven Architecture con Kafka y diseño de sistemas distribuidos tolerantes a fallos.",
        "currentRole": "Tech Lead / Software Architect",
        "estimatedSeniority": "Lead / Architect",
        "totalExperienceYears": 9,
        "skills": [
            {"name": "C# / .NET 10", "normalizedName": ".NET Core / C#", "category": "Backend", "experienceYears": 9, "evidence": "Diseño de arquitectura de microservicios en .NET 8/9/10 con Clean Architecture y DDD", "confidence": 0.98},
            {"name": "Kubernetes & Docker", "normalizedName": "Kubernetes", "category": "DevOps", "experienceYears": 6, "evidence": "Orquestación de clusters productivos con Helm y políticas de autoescalado HPA", "confidence": 0.95},
            {"name": "Apache Kafka", "normalizedName": "Kafka / Event-Driven", "category": "Architecture", "experienceYears": 5, "evidence": "Implementación de arquitectura orientada a eventos procesando más de 2M msgs/día", "confidence": 0.94},
            {"name": "PostgreSQL & Redis", "normalizedName": "PostgreSQL & Redis", "category": "Databases", "experienceYears": 7, "evidence": "Optimización de particionamiento de tablas y caching distribuido de baja latencia", "confidence": 0.96},
            {"name": "Domain-Driven Design", "normalizedName": "Clean Architecture & DDD", "category": "Architecture", "experienceYears": 7, "evidence": "Definición de Bounded Contexts y modelos de dominio desacoplados", "confidence": 0.95}
        ],
        "languages": [
            {"name": "Español", "level": "Nativo", "evidence": "Lengua materna"},
            {"name": "Inglés", "level": "C1 Avanzado", "evidence": "Liderazgo técnico en squads multiculturales en EE.UU."}
        ],
        "education": [
            {"degree": "Licenciatura en Ingeniería de Software", "institution": "Instituto Tecnológico de Santo Domingo (INTEC)", "graduationYear": 2016},
            {"degree": "Maestría en Arquitectura de Software Cloud", "institution": "UNIR", "graduationYear": 2020}
        ],
        "certifications": [
            {"name": "Microsoft Certified: Azure Solutions Architect Expert", "issuer": "Microsoft", "year": 2023},
            {"name": "Certified Kubernetes Administrator (CKA)", "issuer": "Linux Foundation", "year": 2022}
        ],
        "workExperience": [
            {"role": "Principal Solutions Architect", "company": "Innovatech Global", "durationYears": 4, "keyAchievements": ["Lideró la migración de un sistema bancario monolítico a 18 microservicios en .NET", "Redujo el tiempo de inactividad no planificado en un 40% mediante patrones de resiliencia"]},
            {"role": "Senior Backend Tech Lead", "company": "Fintech Solutions", "durationYears": 3, "keyAchievements": ["Diseñó pipeline de pagos transaccionales de alta concurrencia con Kafka y PostgreSQL", "Mentorizó a 12 ingenieros backend en buenas prácticas de TDD y Clean Code"]}
        ],
        "pointsToValidate": [
            "Profundizar en la estrategia de migración de datos transaccionales sin downtime en el proyecto bancario",
            "Validar cómo balancea la deuda técnica frente a la presión de entrega del negocio"
        ],
        "warnings": []
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '2 days'
);

INSERT INTO candidate_disc_results ("Id", "CandidateId", dominance, influence, steadiness, conscientiousness, primary_style, "CompletedAtUtc")
VALUES (
    '11111111-1111-1111-1111-444444444444',
    '11111111-1111-1111-1111-111111111111',
    88,
    62,
    45,
    86,
    'D/C',
    NOW() - INTERVAL '2 days'
);

INSERT INTO candidate_disc_interpretations ("Id", "CandidateId", "DiscResultId", "InterpretationJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '11111111-1111-1111-1111-555555555555',
    '11111111-1111-1111-1111-111111111111',
    '11111111-1111-1111-1111-444444444444',
    $${
        "primaryStyle": "D/C",
        "summary": "Perfil de liderazgo técnico altamente resolutivo, enfocado en el cumplimiento de objetivos estratégicos con estándares rigurosos de calidad arquitectónica y precisión técnica.",
        "strengthsToExplore": [
            "Capacidad destacada para tomar decisiones arquitectónicas críticas bajo incertidumbre",
            "Fuerte orientación al rigor técnico, gobernanza de código y estándares de observabilidad",
            "Habilidad para desafiar el statu quo y elevar el nivel técnico del equipo"
        ],
        "pointsToExplore": [
            "Gestión de la empatía y la paciencia con perfiles junior ante curvas de aprendizaje pronunciadas",
            "Flexibilidad para aceptar compromisos temporales frente a plazos comerciales ajustados"
        ],
        "behavioralQuestionTopics": [
            "Gestión de desacuerdos arquitectónicos con stakeholders de negocio",
            "Liderazgo en situaciones de crisis en entornos de producción"
        ],
        "disclaimer": "Esta síntesis conductual es una herramienta de apoyo para el evaluador y no constituye un diagnóstico clínico."
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '2 days'
);

INSERT INTO candidate_interview_reports ("Id", "CandidateId", "CvAnalysisId", "DiscInterpretationId", "ReportContentJson", "FileUrl", "Status", "Version", "ProviderName", "ModelName", "PromptVersion", "GeneratedAtUtc", "CreatedAtUtc")
VALUES (
    '11111111-1111-1111-1111-666666666666',
    '11111111-1111-1111-1111-111111111111',
    '11111111-1111-1111-1111-333333333333',
    '11111111-1111-1111-1111-555555555555',
    $${
        "candidateOverview": {
            "name": "Sofía Valenzuela Navarro",
            "currentRole": "Tech Lead / Software Architect",
            "experienceYears": 9,
            "professionalSummary": "Arquitecta de Software y Tech Lead con 9 años de experiencia liderando la transformación de monolitos hacia microservicios en .NET 9/10, Kubernetes, Event-Driven Architecture con Kafka y diseño de sistemas distribuidos tolerantes a fallos."
        },
        "professionalProfile": {
            "mainSkills": [".NET Core / C#", "Kubernetes", "Kafka / Event-Driven", "PostgreSQL & Redis", "Clean Architecture & DDD", "Azure Cloud"],
            "relevantExperience": [
                "Principal Solutions Architect en Innovatech Global (4 años)",
                "Senior Backend Tech Lead en Fintech Solutions (3 años)"
            ],
            "education": ["Licenciatura en Ingeniería de Software - INTEC", "Maestría en Arquitectura Cloud - UNIR"],
            "languages": ["Español (Nativo)", "Inglés (C1 Avanzado)"],
            "certifications": ["Azure Solutions Architect Expert", "Certified Kubernetes Administrator (CKA)"]
        },
        "discSummary": {
            "primaryStyle": "D/C",
            "summary": "Perfil de liderazgo técnico altamente resolutivo, enfocado en el cumplimiento de objetivos estratégicos con estándares rigurosos de calidad arquitectónica.",
            "strengthsToExplore": [
                "Toma de decisiones arquitectónicas con alta asertividad",
                "Rigor en la definición y cumplimiento de estándares de código"
            ],
            "pointsToExplore": [
                "Adaptabilidad ante decisiones de negocio que priorizan velocidad sobre perfección arquitectónica"
            ]
        },
        "validationPoints": [
            {
                "topic": "Arquitectura Distribuida",
                "reason": "Profundizar en la estrategia de migración de datos transaccionales sin downtime en el proyecto bancario.",
                "source": "CV"
            },
            {
                "topic": "Estilo de Liderazgo",
                "reason": "Evaluar cómo fomenta la autonomía técnica en desarrolladores de nivel intermedio.",
                "source": "DISC"
            }
        ],
        "interviewGuide": {
            "professionalQuestions": [
                "¿Cuál ha sido la decisión arquitectónica más difícil que tuviste que defender ante el comité ejecutivo y cuál fue su impacto?",
                "¿Cómo defines la frontera de un Bounded Context cuando dos áreas de negocio disputan la pertenencia de una entidad de datos?"
            ],
            "technicalQuestions": [
                "En una arquitectura orientada a eventos con Kafka y .NET 10, ¿cómo garantizas la entrega 'exactly-once' o la idempotencia en los consumidores?",
                "¿Qué patrón utilizas para gestionar transacciones distribuidas entre microservicios (Saga Coreografiada vs. Orquestada) y por qué?",
                "¿Cómo optimizas el uso de conexiones y el pooling en PostgreSQL cuando tienes cientos de réplicas de microservicios en Kubernetes?"
            ],
            "behavioralQuestions": [
                "Cuéntanos sobre una ocasión en la que un miembro de tu equipo no cumplía con los estándares de calidad arquitectónica acordados. ¿Cómo manejaste la conversación y qué resultados obtuviste? (Metodología STAR).",
                "Describe una situación de alta presión durante una caída de producción en un despliegue crítico. ¿Cómo coordinaste la respuesta técnica y la comunicación con el cliente?"
            ]
        },
        "disclaimer": "Este informe sirve como herramienta de apoyo para la entrevista y no reemplaza el criterio profesional del evaluador.",
        "fileUrl": null
    }$$::jsonb,
    NULL,
    'Generated',
    1,
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '2 days',
    NOW() - INTERVAL '2 days'
);

-- =========================================================================================
-- CANDIDATO 2: Carlos Mendoza Rivas - Senior Backend Developer (.NET & PostgreSQL)
-- =========================================================================================
INSERT INTO candidates (
    "Id", "FirstName", "LastName", email, "PhoneNumber",
    "EvaluatorDecision", "EvaluatorNotes", "EvaluatedAtUtc",
    assignedrecruiterid, assignedrecruitername, assignedrecruiteremail, assignedatutc,
    target_role, job_position_id, "CreatedAtUtc"
) VALUES (
    '22222222-2222-2222-2222-111111111111',
    'Carlos',
    'Mendoza Rivas',
    'carlos.mendoza.dev@gmail.com',
    '+1 (809) 555-8812',
    'Pending',
    NULL,
    NULL,
    'carlos.mendoza',
    'Carlos Mendoza',
    'carlos.mendoza@empresa.com',
    NOW() - INTERVAL '1 day',
    'Senior Backend Engineer',
    '11111111-2222-3333-4444-555555555501',
    NOW() - INTERVAL '1 day'

);

INSERT INTO cv_documents ("Id", "CandidateId", "FileName", "StoragePath", "FileSizeBytes", "ContentType", "UploadedAtUtc")
VALUES (
    '22222222-2222-2222-2222-222222222222',
    '22222222-2222-2222-2222-111111111111',
    'CV_Carlos_Mendoza_Backend.pdf',
    '/app/Storage/CV_Carlos_Mendoza_Backend.pdf',
    162000,
    'application/pdf',
    NOW() - INTERVAL '1 day'
);

INSERT INTO candidate_cv_analyses ("Id", "CandidateId", "DocumentId", "AnalysisJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '22222222-2222-2222-2222-333333333333',
    '22222222-2222-2222-2222-111111111111',
    '22222222-2222-2222-2222-222222222222',
    $${
        "professionalSummary": "Desarrollador Backend Senior con 6 años de experiencia construyendo APIs REST de alta concurrencia en ASP.NET Core, optimización de queries SQL en PostgreSQL, caching distribuido con Redis y pipelines CI/CD con GitHub Actions.",
        "currentRole": "Senior Backend Developer",
        "estimatedSeniority": "Senior",
        "totalExperienceYears": 6,
        "skills": [
            {"name": ".NET Core / C#", "normalizedName": ".NET Core / C#", "category": "Backend", "experienceYears": 6, "evidence": "Construcción de APIs RESTful seguras con ASP.NET Core y Entity Framework Core", "confidence": 0.96},
            {"name": "PostgreSQL Performance", "normalizedName": "PostgreSQL Tuning", "category": "Databases", "experienceYears": 5, "evidence": "Optimización de planes de ejecución, índices BRIN/GIN y vistas materializadas", "confidence": 0.94},
            {"name": "Redis Distributed Cache", "normalizedName": "Redis Caching", "category": "Databases", "experienceYears": 4, "evidence": "Implementación de Cache-Aside y pub/sub de mensajería liviana", "confidence": 0.92},
            {"name": "Docker & Containers", "normalizedName": "Docker", "category": "DevOps", "experienceYears": 5, "evidence": "Empaquetado multistage para microservicios .NET optimizados", "confidence": 0.93},
            {"name": "Unit & Integration Testing", "normalizedName": "Testing xUnit / Moq", "category": "Quality", "experienceYears": 5, "evidence": "Cobertura superior al 85% con xUnit, FluentAssertions y Testcontainers", "confidence": 0.95}
        ],
        "languages": [
            {"name": "Español", "level": "Nativo", "evidence": "Lengua materna"},
            {"name": "Inglés", "level": "B2 Profesional", "evidence": "Documentación técnica y standups en inglés"}
        ],
        "education": [
            {"degree": "Ingeniería en Telemática", "institution": "Pontificia Universidad Católica Madre y Maestra (PUCMM)", "graduationYear": 2019}
        ],
        "certifications": [
            {"name": "Microsoft Certified: Azure Developer Associate", "issuer": "Microsoft", "year": 2023}
        ],
        "workExperience": [
            {"role": "Senior Backend Developer", "company": "CloudNexus Dominicana", "durationYears": 3, "keyAchievements": ["Redujo la latencia p99 de 650ms a 85ms optimizando consultas EF Core y Redis", "Diseñó suite de pruebas de integración automatizadas con Testcontainers"]},
            {"role": "Software Developer .NET", "company": "SoftServe Caribe", "durationYears": 3, "keyAchievements": ["Desarrolló módulos de facturación electrónica e integración con pasarelas de pago", "Implementó logging estructurado con Serilog hacia Seq"]}
        ],
        "pointsToValidate": [
            "Comprobar conocimiento en patrones de resiliencia con Polly y circuit breakers en producción",
            "Verificar experiencia manejando bloqueos (locks) y transacciones concurrentes en PostgreSQL"
        ],
        "warnings": []
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '1 day'
);

INSERT INTO candidate_disc_results ("Id", "CandidateId", dominance, influence, steadiness, conscientiousness, primary_style, "CompletedAtUtc")
VALUES (
    '22222222-2222-2222-2222-444444444444',
    '22222222-2222-2222-2222-111111111111',
    48,
    42,
    80,
    92,
    'C/S',
    NOW() - INTERVAL '1 day'
);

INSERT INTO candidate_disc_interpretations ("Id", "CandidateId", "DiscResultId", "InterpretationJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '22222222-2222-2222-2222-555555555555',
    '22222222-2222-2222-2222-111111111111',
    '22222222-2222-2222-2222-444444444444',
    $${
        "primaryStyle": "C/S",
        "summary": "Perfil técnico analítico, metódico y sumamente confiable. Se enfoca en la robustez del código, la cobertura de pruebas, la documentación clara y la estabilidad a largo plazo del sistema.",
        "strengthsToExplore": [
            "Excepcional atención al detalle técnico y calidad de software",
            "Excelente jugador de equipo, consistente y con alta dedicación en tareas complejas de refactorización",
            "Capacidad analítica para depurar problemas oscuros de rendimiento o concurrencia"
        ],
        "pointsToExplore": [
            "Proactividad para comunicar bloqueos o desacuerdos de forma abierta y directa",
            "Velocidad en la toma de decisiones cuando la información es incompleta"
        ],
        "behavioralQuestionTopics": [
            "Manejo de requerimientos cambiantes con poca claridad",
            "Colaboración interdisciplinaria con equipos de frontend y producto"
        ],
        "disclaimer": "Esta síntesis conductual es una herramienta de apoyo para el evaluador."
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '1 day'
);

INSERT INTO candidate_interview_reports ("Id", "CandidateId", "CvAnalysisId", "DiscInterpretationId", "ReportContentJson", "FileUrl", "Status", "Version", "ProviderName", "ModelName", "PromptVersion", "GeneratedAtUtc", "CreatedAtUtc")
VALUES (
    '22222222-2222-2222-2222-666666666666',
    '22222222-2222-2222-2222-111111111111',
    '22222222-2222-2222-2222-333333333333',
    '22222222-2222-2222-2222-555555555555',
    $${
        "candidateOverview": {
            "name": "Carlos Mendoza Rivas",
            "currentRole": "Senior Backend Developer",
            "experienceYears": 6,
            "professionalSummary": "Desarrollador Backend Senior con 6 años de experiencia construyendo APIs REST de alta concurrencia en ASP.NET Core, optimización de queries SQL en PostgreSQL, caching distribuido con Redis y pipelines CI/CD con GitHub Actions."
        },
        "professionalProfile": {
            "mainSkills": [".NET Core / C#", "PostgreSQL Tuning", "Redis Caching", "Docker", "Testing xUnit / Moq", "GitHub Actions"],
            "relevantExperience": [
                "Senior Backend Developer en CloudNexus Dominicana (3 años)",
                "Software Developer .NET en SoftServe Caribe (3 años)"
            ],
            "education": ["Ingeniería en Telemática - PUCMM"],
            "languages": ["Español (Nativo)", "Inglés (B2 Profesional)"],
            "certifications": ["Microsoft Certified: Azure Developer Associate"]
        },
        "discSummary": {
            "primaryStyle": "C/S",
            "summary": "Perfil técnico analítico, metódico y confiable. Enfoque prioritario en robustez, cobertura de pruebas y estabilidad del sistema.",
            "strengthsToExplore": [
                "Rigor técnico y excelencia en diseño de pruebas automatizadas",
                "Constancia y método en optimización de base de datos"
            ],
            "pointsToExplore": [
                "Asertividad para proponer cambios audaces de arquitectura"
            ]
        },
        "validationPoints": [
            {
                "topic": "Rendimiento SQL",
                "reason": "Verificar experiencia manejando bloqueos (locks) y transacciones concurrentes en PostgreSQL.",
                "source": "CV"
            },
            {
                "topic": "Resiliencia Backend",
                "reason": "Comprobar conocimiento en patrones de resiliencia con Polly y circuit breakers en producción.",
                "source": "CV"
            }
        ],
        "interviewGuide": {
            "professionalQuestions": [
                "¿Qué técnicas utilizaste en CloudNexus para detectar los cuellos de botella que causaban la latencia p99 de 650ms?",
                "¿Cómo organizas la estructura de un proyecto .NET para aislar la lógica de dominio de las dependencias de infraestructura y base de datos?"
            ],
            "technicalQuestions": [
                "¿Cómo resuelves el problema de 'N+1 queries' en Entity Framework Core y cuándo prefieres usar Dapper o SQL puro?",
                "¿Cómo configuras políticas de Retry con Exponential Backoff y Circuit Breaker con Polly para llamadas HTTP salientes?",
                "Explícanos cómo gestionas las migraciones de base de datos en entornos de producción con cero downtime."
            ],
            "behavioralQuestions": [
                "Cuéntanos sobre una situación donde tuviste que refactorizar un módulo de código crítico y desordenado sin romper funcionalidad existente. ¿Cómo planificaste el trabajo y qué pruebas implementaste? (Metodología STAR).",
                "Describe una ocasión en la que recibiste retroalimentación técnica crítica en un Code Review. ¿Cómo la procesaste y qué acciones tomaste?"
            ]
        },
        "disclaimer": "Este informe sirve como herramienta de apoyo para la entrevista y no reemplaza el criterio profesional del evaluador.",
        "fileUrl": null
    }$$::jsonb,
    NULL,
    'Generated',
    1,
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '1 day',
    NOW() - INTERVAL '1 day'
);

-- =========================================================================================
-- CANDIDATO 3: Valeria Herrera Morales - Senior Fullstack Engineer (React & .NET)
-- =========================================================================================
INSERT INTO candidates (
    "Id", "FirstName", "LastName", email, "PhoneNumber",
    "EvaluatorDecision", "EvaluatorNotes", "EvaluatedAtUtc",
    assignedrecruiterid, assignedrecruitername, assignedrecruiteremail, assignedatutc,
    target_role, job_position_id, "CreatedAtUtc"
) VALUES (
    '33333333-3333-3333-3333-111111111111',
    'Valeria',
    'Herrera Morales',
    'valeria.herrera@fullstack.io',
    '+1 (829) 555-7319',
    'Pending',
    NULL,
    NULL,
    'laura.sanchez',
    'Laura Sánchez',
    'laura.sanchez@empresa.com',
    NOW() - INTERVAL '18 hours',
    'Fullstack Developer',
    '11111111-2222-3333-4444-555555555504',
    NOW() - INTERVAL '18 hours'

);

INSERT INTO cv_documents ("Id", "CandidateId", "FileName", "StoragePath", "FileSizeBytes", "ContentType", "UploadedAtUtc")
VALUES (
    '33333333-3333-3333-3333-222222222222',
    '33333333-3333-3333-3333-111111111111',
    'CV_Valeria_Herrera_Fullstack.pdf',
    '/app/Storage/CV_Valeria_Herrera_Fullstack.pdf',
    171000,
    'application/pdf',
    NOW() - INTERVAL '18 hours'
);

INSERT INTO candidate_cv_analyses ("Id", "CandidateId", "DocumentId", "AnalysisJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '33333333-3333-3333-3333-333333333333',
    '33333333-3333-3333-3333-111111111111',
    '33333333-3333-3333-3333-222222222222',
    $${
        "professionalSummary": "Ingeniera Fullstack Senior con 5 años de experiencia diseñando aplicaciones web dinámicas y escalables con React 19, TypeScript, TailwindCSS, Zustand y backends robustos en ASP.NET Core y PostgreSQL.",
        "currentRole": "Senior Fullstack Engineer",
        "estimatedSeniority": "Senior",
        "totalExperienceYears": 5,
        "skills": [
            {"name": "React 19 & TypeScript", "normalizedName": "React & TypeScript", "category": "Frontend", "experienceYears": 5, "evidence": "Desarrollo de portales SPA y dashboards con Server State (TanStack Query) y Vite", "confidence": 0.97},
            {"name": "TailwindCSS & UI/UX", "normalizedName": "TailwindCSS", "category": "Frontend", "experienceYears": 4, "evidence": "Diseño de sistemas de diseño corporativos con accesibilidad WCAG AA", "confidence": 0.95},
            {"name": "ASP.NET Core Web API", "normalizedName": "ASP.NET Core API", "category": "Backend", "experienceYears": 4, "evidence": "Creación de endpoints RESTful seguros con JWT y control de acceso RBAC", "confidence": 0.93},
            {"name": "State Management (Zustand/Redux)", "normalizedName": "State Management", "category": "Frontend", "experienceYears": 4, "evidence": "Gestión de estado global reactivo y optimización de renderizados innecesarios", "confidence": 0.94},
            {"name": "PostgreSQL & EF Core", "normalizedName": "PostgreSQL & EF Core", "category": "Databases", "experienceYears": 4, "evidence": "Modelado de datos relacionales y optimización de consultas complejas", "confidence": 0.92}
        ],
        "languages": [
            {"name": "Español", "level": "Nativo", "evidence": "Lengua materna"},
            {"name": "Inglés", "level": "B2+ Avanzado", "evidence": "Interacción con clientes en Norteamérica"}
        ],
        "education": [
            {"degree": "Licenciatura en Ciencias de la Computación", "institution": "Universidad Autónoma de Santo Domingo (UASD)", "graduationYear": 2020}
        ],
        "certifications": [
            {"name": "Meta Front-End Developer Professional Certificate", "issuer": "Coursera / Meta", "year": 2022}
        ],
        "workExperience": [
            {"role": "Lead Fullstack Developer", "company": "SaaS Factory Labs", "durationYears": 2, "keyAchievements": ["Lideró el rediseño del producto principal logrando 45% de mejora en Core Web Vitals (LCP < 1.2s)", "Implementó arquitectura frontend modular basada en micro-frontends"]},
            {"role": "Fullstack Software Engineer", "company": "Global Tech Services", "durationYears": 3, "keyAchievements": ["Desarrolló plataforma de e-commerce procesando más de $2M mensuales", "Automatizó despliegues continuos con Docker y GitHub Actions"]}
        ],
        "pointsToValidate": [
            "Validar experiencia profunda en SSR/SSG con Next.js vs Vite SPA tradicional",
            "Explorar su enfoque para pruebas end-to-end (Playwright/Cypress) en el frontend"
        ],
        "warnings": []
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '18 hours'
);

INSERT INTO candidate_disc_results ("Id", "CandidateId", dominance, influence, steadiness, conscientiousness, primary_style, "CompletedAtUtc")
VALUES (
    '33333333-3333-3333-3333-444444444444',
    '33333333-3333-3333-3333-111111111111',
    76,
    88,
    52,
    62,
    'I/D',
    NOW() - INTERVAL '18 hours'
);

INSERT INTO candidate_disc_interpretations ("Id", "CandidateId", "DiscResultId", "InterpretationJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '33333333-3333-3333-3333-555555555555',
    '33333333-3333-3333-3333-111111111111',
    '33333333-3333-3333-3333-444444444444',
    $${
        "primaryStyle": "I/D",
        "summary": "Perfil dinámico, carismático y con alta capacidad de persuasión e iniciativa. Excelente comunicadora, con talento natural para conectar las necesidades del usuario con soluciones técnicas innovadoras.",
        "strengthsToExplore": [
            "Habilidad sobresaliente para colaborar con diseñadores UX/UI y stakeholders de producto",
            "Alta energía, proactividad y liderazgo motivacional en el equipo",
            "Capacidad para presentar demostraciones técnicas de forma clara y atractiva"
        ],
        "pointsToExplore": [
            "Atención al detalle en tareas repetitivas o de documentación exhaustiva",
            "Gestión del tiempo y establecimiento de prioridades ante múltiples requerimientos simultáneos"
        ],
        "behavioralQuestionTopics": [
            "Resolución de conflictos de diseño con equipos de Producto",
            "Manejo de entregas bajo plazos ajustados con cambio de alcance"
        ],
        "disclaimer": "Esta síntesis conductual es una herramienta de apoyo para el evaluador."
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '18 hours'
);

INSERT INTO candidate_interview_reports ("Id", "CandidateId", "CvAnalysisId", "DiscInterpretationId", "ReportContentJson", "FileUrl", "Status", "Version", "ProviderName", "ModelName", "PromptVersion", "GeneratedAtUtc", "CreatedAtUtc")
VALUES (
    '33333333-3333-3333-3333-666666666666',
    '33333333-3333-3333-3333-111111111111',
    '33333333-3333-3333-3333-333333333333',
    '33333333-3333-3333-3333-555555555555',
    $${
        "candidateOverview": {
            "name": "Valeria Herrera Morales",
            "currentRole": "Senior Fullstack Engineer",
            "experienceYears": 5,
            "professionalSummary": "Ingeniera Fullstack Senior con 5 años de experiencia diseñando aplicaciones web dinámicas y escalables con React 19, TypeScript, TailwindCSS, Zustand y backends robustos en ASP.NET Core y PostgreSQL."
        },
        "professionalProfile": {
            "mainSkills": ["React & TypeScript", "TailwindCSS", "ASP.NET Core API", "State Management", "PostgreSQL & EF Core", "Docker"],
            "relevantExperience": [
                "Lead Fullstack Developer en SaaS Factory Labs (2 años)",
                "Fullstack Software Engineer en Global Tech Services (3 años)"
            ],
            "education": ["Licenciatura en Ciencias de la Computación - UASD"],
            "languages": ["Español (Nativo)", "Inglés (B2+ Avanzado)"],
            "certifications": ["Meta Front-End Developer Professional Certificate"]
        },
        "discSummary": {
            "primaryStyle": "I/D",
            "summary": "Perfil dinámico con alta capacidad de persuasión y liderazgo motivacional. Fuerte foco en la experiencia de usuario y agilidad.",
            "strengthsToExplore": [
                "Comunicación fluida y enlace efectivo con equipos de producto y diseño",
                "Rapidez para prototipar e iterar soluciones visuales interactivas"
            ],
            "pointsToExplore": [
                "Profundidad en diseño de arquitectura de testing automatizado"
            ]
        },
        "validationPoints": [
            {
                "topic": "Frontend Performance",
                "reason": "Detallar cómo logró mejorar los Core Web Vitals en un 45% (LCP < 1.2s).",
                "source": "CV"
            },
            {
                "topic": "Colaboración Interdisciplinaria",
                "reason": "Explorar cómo negocia con los diseñadores de UI cuando una propuesta es técnicamente inviable.",
                "source": "DISC"
            }
        ],
        "interviewGuide": {
            "professionalQuestions": [
                "¿Cómo organizas la sincronización de estado entre servidor y cliente (Server State vs. Client State) en aplicaciones React complejas?",
                "¿Qué estrategia utilizas para asegurar que los contratos de API en .NET Core coincidan exactamente con los tipos TypeScript en el frontend?"
            ],
            "technicalQuestions": [
                "¿Qué técnicas de optimización de rendimiento aplicas en React 19 para evitar renderizados innecesarios y reducir el bundle size?",
                "¿Cómo manejas la autenticación y renovación de tokens JWT en el frontend para evitar condiciones de carrera en peticiones paralelas?",
                "¿Cómo estructuras un componente modal o drawer accesible con control de foco y navegación por teclado en TailwindCSS?"
            ],
            "behavioralQuestions": [
                "Describe una situación donde el equipo de Producto solicitó un cambio radical de interfaz a 3 días del lanzamiento. ¿Cómo respondiste y cómo organizaste las prioridades? (Metodología STAR).",
                "Cuéntanos sobre una ocasión en la que tuviste que convencer a otros desarrolladores de adoptar una nueva herramienta o convención de código."
            ]
        },
        "disclaimer": "Este informe sirve como herramienta de apoyo para la entrevista y no reemplaza el criterio profesional del evaluador.",
        "fileUrl": null
    }$$::jsonb,
    NULL,
    'Generated',
    1,
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '18 hours',
    NOW() - INTERVAL '18 hours'
);

-- =========================================================================================
-- CANDIDATO 4: Alejandro Gómez Castillo - DevOps & Cloud Infrastructure Engineer
-- =========================================================================================
INSERT INTO candidates (
    "Id", "FirstName", "LastName", email, "PhoneNumber",
    "EvaluatorDecision", "EvaluatorNotes", "EvaluatedAtUtc",
    assignedrecruiterid, assignedrecruitername, assignedrecruiteremail, assignedatutc,
    target_role, job_position_id, "CreatedAtUtc"
) VALUES (
    '44444444-4444-4444-4444-111111111111',
    'Alejandro',
    'Gómez Castillo',
    'alejandro.gomez@cloudinfra.net',
    '+1 (809) 555-9044',
    'Pending',
    NULL,
    NULL,
    'carlos.mendoza',
    'Carlos Mendoza',
    'carlos.mendoza@empresa.com',
    NOW() - INTERVAL '10 hours',
    'Lead Cloud Architect',
    '11111111-2222-3333-4444-555555555502',
    NOW() - INTERVAL '12 hours'

);

INSERT INTO cv_documents ("Id", "CandidateId", "FileName", "StoragePath", "FileSizeBytes", "ContentType", "UploadedAtUtc")
VALUES (
    '44444444-4444-4444-4444-222222222222',
    '44444444-4444-4444-4444-111111111111',
    'CV_Alejandro_Gomez_DevOps.pdf',
    '/app/Storage/CV_Alejandro_Gomez_DevOps.pdf',
    195000,
    'application/pdf',
    NOW() - INTERVAL '12 hours'
);

INSERT INTO candidate_cv_analyses ("Id", "CandidateId", "DocumentId", "AnalysisJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '44444444-4444-4444-4444-333333333333',
    '44444444-4444-4444-4444-111111111111',
    '44444444-4444-4444-4444-222222222222',
    $${
        "professionalSummary": "Ingeniero DevOps & Cloud Infrastructure con 7 años de experiencia implementando Infraestructura como Código (Terraform), despliegue de clusters Kubernetes (EKS/AKS), observabilidad avanzada con Prometheus/Seq/Grafana y prácticas de DevSecOps.",
        "currentRole": "DevOps & Cloud Infrastructure Engineer",
        "estimatedSeniority": "Senior",
        "totalExperienceYears": 7,
        "skills": [
            {"name": "Kubernetes & Helm", "normalizedName": "Kubernetes & Helm", "category": "DevOps", "experienceYears": 6, "evidence": "Gestión de clusters multirregión con ingress controllers, cert-manager y Calico CNI", "confidence": 0.98},
            {"name": "Terraform / OpenTofu", "normalizedName": "Terraform IaC", "category": "Cloud", "experienceYears": 5, "evidence": "Módulos reutilizables para aprovisionamiento de VPC, RDS, AKS y seguridad IAM", "confidence": 0.96},
            {"name": "CI/CD GitHub Actions & GitLab", "normalizedName": "CI/CD Pipelines", "category": "DevOps", "experienceYears": 7, "evidence": "Construcción de pipelines con escaneo SAST/DAST y despliegues Canary con ArgoCD", "confidence": 0.95},
            {"name": "Observabilidad & Logging", "normalizedName": "Prometheus & Seq", "category": "Monitoring", "experienceYears": 5, "evidence": "Configuración de dashboards de SLO/SLA, alertas PagerDuty y tracing OpenTelemetry", "confidence": 0.94},
            {"name": "Cloud AWS & Azure", "normalizedName": "AWS & Azure Cloud", "category": "Cloud", "experienceYears": 6, "evidence": "Diseño de arquitecturas cloud seguras conforme a normas SOC2 e ISO 27001", "confidence": 0.95}
        ],
        "languages": [
            {"name": "Español", "level": "Nativo", "evidence": "Lengua materna"},
            {"name": "Inglés", "level": "C1 Avanzado", "evidence": "Certificación TOEFL iBT 105 puntos"}
        ],
        "education": [
            {"degree": "Ingeniería en Sistemas de Información", "institution": "Universidad Nacional Pedro Henríquez Ureña (UNPHU)", "graduationYear": 2018}
        ],
        "certifications": [
            {"name": "AWS Certified DevOps Engineer - Professional", "issuer": "Amazon Web Services", "year": 2023},
            {"name": "HashiCorp Certified: Terraform Associate", "issuer": "HashiCorp", "year": 2022}
        ],
        "workExperience": [
            {"role": "Lead DevOps & Platform Engineer", "company": "KubeCloud Enterprise", "durationYears": 4, "keyAchievements": ["Automatizó el 100% de la infraestructura mediante Terraform reduciendo tiempos de despliegue en un 70%", "Implementó GitOps con ArgoCD para 35 microservicios en Kubernetes"]},
            {"role": "Cloud Infrastructure Engineer", "company": "Caribe Datacenter", "durationYears": 3, "keyAchievements": ["Administró infraestructuras híbridas con VMware, AWS y redes VPN/BGP", "Configuró sistemas de backups inmutables y planes de Disaster Recovery (RPO < 15min)"]}
        ],
        "pointsToValidate": [
            "Validar experiencia en resolución de incidentes de seguridad y respuesta a brechas en la nube",
            "Profundizar en costos y optimización de FinOps en entornos multi-cloud"
        ],
        "warnings": []
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '12 hours'
);

INSERT INTO candidate_disc_results ("Id", "CandidateId", dominance, influence, steadiness, conscientiousness, primary_style, "CompletedAtUtc")
VALUES (
    '44444444-4444-4444-4444-444444444444',
    '44444444-4444-4444-4444-111111111111',
    72,
    46,
    58,
    90,
    'C/D',
    NOW() - INTERVAL '12 hours'
);

INSERT INTO candidate_disc_interpretations ("Id", "CandidateId", "DiscResultId", "InterpretationJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '44444444-4444-4444-4444-555555555555',
    '44444444-4444-4444-4444-111111111111',
    '44444444-4444-4444-4444-444444444444',
    $${
        "primaryStyle": "C/D",
        "summary": "Perfil orientado a la excelencia técnica, la seguridad y el control operacional riguroso. Excelente capacidad de reacción y toma de decisiones firmes ante emergencias e incidentes críticos de infraestructura.",
        "strengthsToExplore": [
            "Capacidad para estructurar procesos de despliegue automatizados y seguros con cero tolerancia a fallas",
            "Enfoque metódico en el análisis de causa raíz (RCA) tras incidentes de producción",
            "Firmeza para hacer cumplir las políticas de seguridad y gobernanza en la nube"
        ],
        "pointsToExplore": [
            "Flexibilidad para equilibrar la seguridad con la velocidad requerida por los desarrolladores",
            "Habilidades pedagógicas para capacitar a los equipos de desarrollo en cultura DevOps"
        ],
        "behavioralQuestionTopics": [
            "Gestión de guardia (On-call) y mitigación de fatiga de alertas",
            "Manejo de fricciones entre Operaciones y Desarrollo"
        ],
        "disclaimer": "Esta síntesis conductual es una herramienta de apoyo para el evaluador."
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '12 hours'
);

INSERT INTO candidate_interview_reports ("Id", "CandidateId", "CvAnalysisId", "DiscInterpretationId", "ReportContentJson", "FileUrl", "Status", "Version", "ProviderName", "ModelName", "PromptVersion", "GeneratedAtUtc", "CreatedAtUtc")
VALUES (
    '44444444-4444-4444-4444-666666666666',
    '44444444-4444-4444-4444-111111111111',
    '44444444-4444-4444-4444-333333333333',
    '44444444-4444-4444-4444-555555555555',
    $${
        "candidateOverview": {
            "name": "Alejandro Gómez Castillo",
            "currentRole": "DevOps & Cloud Infrastructure Engineer",
            "experienceYears": 7,
            "professionalSummary": "Ingeniero DevOps & Cloud Infrastructure con 7 años de experiencia implementando Infraestructura como Código (Terraform), despliegue de clusters Kubernetes (EKS/AKS), observabilidad avanzada con Prometheus/Seq/Grafana y prácticas de DevSecOps."
        },
        "professionalProfile": {
            "mainSkills": ["Kubernetes & Helm", "Terraform IaC", "CI/CD Pipelines", "Prometheus & Seq", "AWS & Azure Cloud", "GitOps ArgoCD"],
            "relevantExperience": [
                "Lead DevOps & Platform Engineer en KubeCloud Enterprise (4 años)",
                "Cloud Infrastructure Engineer en Caribe Datacenter (3 años)"
            ],
            "education": ["Ingeniería en Sistemas de Información - UNPHU"],
            "languages": ["Español (Nativo)", "Inglés (C1 Avanzado)"],
            "certifications": ["AWS Certified DevOps Engineer - Professional", "Terraform Associate"]
        },
        "discSummary": {
            "primaryStyle": "C/D",
            "summary": "Perfil orientado a la excelencia técnica, seguridad y control operacional. Alta capacidad de resolución metódica ante incidentes.",
            "strengthsToExplore": [
                "Dominio sólido de automatización GitOps e Infraestructura como Código",
                "Rigor en análisis de causa raíz y políticas de observabilidad"
            ],
            "pointsToExplore": [
                "Estrategias para evangelizar prácticas DevOps sin generar cuellos de botella en el equipo"
            ]
        },
        "validationPoints": [
            {
                "topic": "Gestión de Incidentes",
                "reason": "Profundizar en un incidente real de producción de alta severidad que haya tenido que liderar.",
                "source": "CV"
            },
            {
                "topic": "Optimización de Costos (FinOps)",
                "reason": "Validar qué estrategias ha utilizado para reducir la factura de nube sin degradar el rendimiento.",
                "source": "CV"
            }
        ],
        "interviewGuide": {
            "professionalQuestions": [
                "¿Cómo organizas la estructura de carpetas y el estado remoto (Remote State & Locking) en Terraform cuando varios equipos colaboran simultáneamente?",
                "¿Qué métricas doradas (Golden Signals) monitoreas en tus clusters Kubernetes para anticipar problemas de capacidad o cuellos de botella?"
            ],
            "technicalQuestions": [
                "¿Cómo configuras una estrategia de despliegue Canary o Blue-Green en Kubernetes utilizando ArgoCD / Flagger e Ingress Controllers?",
                "Si un nodo de Kubernetes entra en estado 'NotReady' durante un pico de tráfico, ¿cuál es tu procedimiento paso a paso para diagnosticar y aislar la falla?",
                "¿Cómo integras el análisis de vulnerabilidades en imágenes de contenedores (con Trivy o Grype) en el pipeline de CI sin retrasar innecesariamente el ciclo de feedback?"
            ],
            "behavioralQuestions": [
                "Describe una ocasión en la que una alerta crítica interrumpió tu fin de semana. ¿Cómo procediste para diagnosticar la causa raíz y qué cambios implementaste para evitar que se repitiera? (Metodología STAR).",
                "Cuéntanos sobre una situación en la que tuviste que frenar un pase a producción por riesgos de seguridad detectados a última hora. ¿Cómo lo comunicaste al equipo de desarrollo y liderazgo?"
            ]
        },
        "disclaimer": "Este informe sirve como herramienta de apoyo para la entrevista y no reemplaza el criterio profesional del evaluador.",
        "fileUrl": null
    }$$::jsonb,
    NULL,
    'Generated',
    1,
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '12 hours',
    NOW() - INTERVAL '12 hours'
);

-- =========================================================================================
-- CANDIDATO 5: Mariana Pineda Ruiz - Senior Data Engineer & Analytics
-- =========================================================================================
INSERT INTO candidates (
    "Id", "FirstName", "LastName", email, "PhoneNumber",
    "EvaluatorDecision", "EvaluatorNotes", "EvaluatedAtUtc",
    assignedrecruiterid, assignedrecruitername, assignedrecruiteremail, assignedatutc,
    target_role, job_position_id, "CreatedAtUtc"
) VALUES (
    '55555555-5555-5555-5555-111111111111',
    'Mariana',
    'Pineda Ruiz',
    'mariana.pineda@dataops.tech',
    '+1 (849) 555-3210',
    'Pending',
    NULL,
    NULL,
    'laura.sanchez',
    'Laura Sánchez',
    'laura.sanchez@empresa.com',
    NOW() - INTERVAL '6 hours',
    'Data Engineer',
    '11111111-2222-3333-4444-555555555503',
    NOW() - INTERVAL '6 hours'

);

INSERT INTO cv_documents ("Id", "CandidateId", "FileName", "StoragePath", "FileSizeBytes", "ContentType", "UploadedAtUtc")
VALUES (
    '55555555-5555-5555-5555-222222222222',
    '55555555-5555-5555-5555-111111111111',
    'CV_Mariana_Pineda_DataEngineer.pdf',
    '/app/Storage/CV_Mariana_Pineda_DataEngineer.pdf',
    178000,
    'application/pdf',
    NOW() - INTERVAL '6 hours'
);

INSERT INTO candidate_cv_analyses ("Id", "CandidateId", "DocumentId", "AnalysisJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '55555555-5555-5555-5555-333333333333',
    '55555555-5555-5555-5555-111111111111',
    '55555555-5555-5555-5555-222222222222',
    $${
        "professionalSummary": "Ingeniera de Datos Senior con 6 años de experiencia diseñando Data Pipelines escalables, modelado dimensional en Data Warehouses (BigQuery/PostgreSQL), orquestación con n8n/Airflow y transformaciones con dbt y Python.",
        "currentRole": "Senior Data Engineer",
        "estimatedSeniority": "Senior",
        "totalExperienceYears": 6,
        "skills": [
            {"name": "Python Data Stack (Pandas/Polars/PySpark)", "normalizedName": "Python Data", "category": "Data", "experienceYears": 6, "evidence": "Procesamiento de grandes volúmenes de datos con Polars y scripts de limpieza automatizada", "confidence": 0.97},
            {"name": "SQL & PostgreSQL / BigQuery", "normalizedName": "PostgreSQL & BigQuery", "category": "Databases", "experienceYears": 6, "evidence": "Modelado dimensional estrella/copo de nieve y optimización de particiones", "confidence": 0.96},
            {"name": "ETL/ELT Orchestration (n8n/Airflow)", "normalizedName": "ETL / n8n / Airflow", "category": "Data", "experienceYears": 4, "evidence": "Orquestación de pipelines automatizados con integración a webhooks y APIs REST", "confidence": 0.94},
            {"name": "dbt (Data Build Tool)", "normalizedName": "dbt & Dataform", "category": "Data", "experienceYears": 3, "evidence": "Modelado declarativo con pruebas automatizadas de calidad de datos (Great Expectations)", "confidence": 0.92},
            {"name": "Docker & Cloud Data Storage", "normalizedName": "Docker & Cloud Storage", "category": "Infrastructure", "experienceYears": 4, "evidence": "Contenedorización de jobs batch y almacenamiento en buckets cloud", "confidence": 0.93}
        ],
        "languages": [
            {"name": "Español", "level": "Nativo", "evidence": "Lengua materna"},
            {"name": "Inglés", "level": "B2 Avanzado", "evidence": "Comunicación con equipos globales de Analytics"}
        ],
        "education": [
            {"degree": "Licenciatura en Matemáticas y Computación", "institution": "INTEC", "graduationYear": 2019}
        ],
        "certifications": [
            {"name": "Google Cloud Professional Data Engineer", "issuer": "Google Cloud", "year": 2023}
        ],
        "workExperience": [
            {"role": "Senior Data Engineer", "company": "DataVanguard Analytics", "durationYears": 3, "keyAchievements": ["Diseñó pipeline de analítica en tiempo real ingiriendo más de 50GB diarios", "Implementó data quality checks automatizados con dbt reduciendo anomalías en reportes en un 60%"]},
            {"role": "BI & Data Analyst Developer", "company": "Retail Group Dominicana", "durationYears": 3, "keyAchievements": ["Construyó Data Marts para análisis de ventas y comportamiento del consumidor", "Automatizó reportes ejecutivos integrados con n8n y Slack"]}
        ],
        "pointsToValidate": [
            "Validar experiencia en streaming (Kafka/PubSub) vs procesamiento batch tradicional",
            "Explorar gobierno de datos y cumplimiento de privacidad (GDPR/Habeas Data)"
        ],
        "warnings": []
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '6 hours'
);

INSERT INTO candidate_disc_results ("Id", "CandidateId", dominance, influence, steadiness, conscientiousness, primary_style, "CompletedAtUtc")
VALUES (
    '55555555-5555-5555-5555-444444444444',
    '55555555-5555-5555-5555-111111111111',
    44,
    52,
    86,
    88,
    'S/C',
    NOW() - INTERVAL '6 hours'
);

INSERT INTO candidate_disc_interpretations ("Id", "CandidateId", "DiscResultId", "InterpretationJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '55555555-5555-5555-5555-555555555555',
    '55555555-5555-5555-5555-111111111111',
    '55555555-5555-5555-5555-444444444444',
    $${
        "primaryStyle": "S/C",
        "summary": "Perfil paciente, altamente estructurado y con gran sentido de la responsabilidad. Excelente capacidad de concentración en tareas complejas de modelado y depuración de datos, priorizando la consistencia y la veracidad de la información.",
        "strengthsToExplore": [
            "Rigor en la validación y limpieza de datos antes de disponibilizarlos al negocio",
            "Paciencia y perseverancia en la resolución de inconsistencias de datos históricas",
            "Excelente disposición para documentar catálogos de datos y apoyar a analistas de negocio"
        ],
        "pointsToExplore": [
            "Capacidad para manejar cambios constantes de prioridades en entornos de ritmo acelerado",
            "Proactividad para proponer nuevas tecnologías cuando las existentes se vuelven obsoletas"
        ],
        "behavioralQuestionTopics": [
            "Manejo de discrepancias en datos reportadas por usuarios clave",
            "Gestión de proyectos con fechas límites estrictas"
        ],
        "disclaimer": "Esta síntesis conductual es una herramienta de apoyo para el evaluador."
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '6 hours'
);

INSERT INTO candidate_interview_reports ("Id", "CandidateId", "CvAnalysisId", "DiscInterpretationId", "ReportContentJson", "FileUrl", "Status", "Version", "ProviderName", "ModelName", "PromptVersion", "GeneratedAtUtc", "CreatedAtUtc")
VALUES (
    '55555555-5555-5555-5555-666666666666',
    '55555555-5555-5555-5555-111111111111',
    '55555555-5555-5555-5555-333333333333',
    '55555555-5555-5555-5555-555555555555',
    $${
        "candidateOverview": {
            "name": "Mariana Pineda Ruiz",
            "currentRole": "Senior Data Engineer",
            "experienceYears": 6,
            "professionalSummary": "Ingeniera de Datos Senior con 6 años de experiencia diseñando Data Pipelines escalables, modelado dimensional en Data Warehouses (BigQuery/PostgreSQL), orquestación con n8n/Airflow y transformaciones con dbt y Python."
        },
        "professionalProfile": {
            "mainSkills": ["Python Data", "PostgreSQL & BigQuery", "ETL / n8n / Airflow", "dbt & Dataform", "Docker & Cloud Storage"],
            "relevantExperience": [
                "Senior Data Engineer en DataVanguard Analytics (3 años)",
                "BI & Data Analyst Developer en Retail Group Dominicana (3 años)"
            ],
            "education": ["Licenciatura en Matemáticas y Computación - INTEC"],
            "languages": ["Español (Nativo)", "Inglés (B2 Avanzado)"],
            "certifications": ["Google Cloud Professional Data Engineer"]
        },
        "discSummary": {
            "primaryStyle": "S/C",
            "summary": "Perfil estructurado, confiable y metódico. Foco prioritario en la calidad, integridad y consistencia de los modelos de datos.",
            "strengthsToExplore": [
                "Calidad y exactitud en modelado de datos y pruebas de validación",
                "Constancia y excelente colaboración con equipos de analítica y negocio"
            ],
            "pointsToExplore": [
                "Flexibilidad ante cambios repentinos de esquemas en orígenes no controlados"
            ]
        },
        "validationPoints": [
            {
                "topic": "Orquestación de Pipelines",
                "reason": "Explicar cómo maneja la reintentabilidad y recuperación ante fallos en pipelines automatizados.",
                "source": "CV"
            },
            {
                "topic": "Calidad de Datos",
                "reason": "Detallar el framework de data quality checks implementado con dbt.",
                "source": "CV"
            }
        ],
        "interviewGuide": {
            "professionalQuestions": [
                "¿Cómo diseñas una estrategia de Slowly Changing Dimensions (SCD Tipo 2) en PostgreSQL / Data Warehouse para mantener el historial de cambios?",
                "¿Qué consideraciones tomas en cuenta al migrar una transformación batch a un pipeline casi en tiempo real?"
            ],
            "technicalQuestions": [
                "¿Cómo optimizas una consulta analítica en PostgreSQL con tablas de más de 50 millones de registros usando particiones e índices adecuados?",
                "¿Cómo gestionas la idempotencia en tus jobs de ETL/ELT para permitir reejecuciones sin generar duplicados de datos?",
                "¿Qué buenas prácticas aplicas en dbt para testing de modelos y documentación automática del catálogo de datos?"
            ],
            "behavioralQuestions": [
                "Cuéntanos sobre una ocasión en la que descubriste un error crítico en una métrica financiera histórica tras un cambio de esquema. ¿Cómo lo abordaste y cómo lo comunicaste a los líderes de negocio? (Metodología STAR).",
                "Describe una situación donde tuviste que coordinar con ingenieros de software que cambiaron la estructura de una base de datos transaccional sin previo aviso."
            ]
        },
        "disclaimer": "Este informe sirve como herramienta de apoyo para la entrevista y no reemplaza el criterio profesional del evaluador.",
        "fileUrl": null
    }$$::jsonb,
    NULL,
    'Generated',
    1,
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '6 hours',
    NOW() - INTERVAL '6 hours'
);

-- =========================================================================================
-- CANDIDATO 6: David Rangel Soto - QA Automation Engineer (SDET)
-- =========================================================================================
INSERT INTO candidates (
    "Id", "FirstName", "LastName", email, "PhoneNumber",
    "EvaluatorDecision", "EvaluatorNotes", "EvaluatedAtUtc",
    assignedrecruiterid, assignedrecruitername, assignedrecruiteremail, assignedatutc,
    target_role, job_position_id, "CreatedAtUtc"
) VALUES (
    '66666666-6666-6666-6666-111111111111',
    'David',
    'Rangel Soto',
    'david.rangel.qa@testing.dev',
    '+1 (809) 555-6123',
    'Pending',
    NULL,
    NULL,
    'carlos.mendoza',
    'Carlos Mendoza',
    'carlos.mendoza@empresa.com',
    NOW() - INTERVAL '2 hours',
    'Senior Backend Engineer',
    '11111111-2222-3333-4444-555555555501',
    NOW() - INTERVAL '2 hours'

);

INSERT INTO cv_documents ("Id", "CandidateId", "FileName", "StoragePath", "FileSizeBytes", "ContentType", "UploadedAtUtc")
VALUES (
    '66666666-6666-6666-6666-222222222222',
    '66666666-6666-6666-6666-111111111111',
    'CV_David_Rangel_QA_Automation.pdf',
    '/app/Storage/CV_David_Rangel_QA_Automation.pdf',
    155000,
    'application/pdf',
    NOW() - INTERVAL '2 hours'
);

INSERT INTO candidate_cv_analyses ("Id", "CandidateId", "DocumentId", "AnalysisJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '66666666-6666-6666-6666-333333333333',
    '66666666-6666-6666-6666-111111111111',
    '66666666-6666-6666-6666-222222222222',
    $${
        "professionalSummary": "Ingeniero QA Automation (SDET) con 4 años de experiencia diseñando frameworks de pruebas automatizadas E2E con Playwright y Cypress, pruebas de rendimiento con k6 y testing de APIs REST en pipelines de CI/CD.",
        "currentRole": "QA Automation Engineer",
        "estimatedSeniority": "Mid-Senior",
        "totalExperienceYears": 4,
        "skills": [
            {"name": "Playwright & Cypress (TypeScript)", "normalizedName": "Playwright & Cypress", "category": "Testing", "experienceYears": 4, "evidence": "Diseño de suite E2E con Page Object Model y paralelización en GitHub Actions", "confidence": 0.96},
            {"name": "API Testing (Postman / RestSharp)", "normalizedName": "API Testing", "category": "Testing", "experienceYears": 4, "evidence": "Automatización de pruebas de contratos y seguridad en endpoints REST", "confidence": 0.95},
            {"name": "Performance Testing (k6 / JMeter)", "normalizedName": "k6 Load Testing", "category": "Testing", "experienceYears": 3, "evidence": "Simulación de escenarios de carga con más de 5,000 usuarios concurrentes", "confidence": 0.93},
            {"name": "Docker & CI/CD Integration", "normalizedName": "Docker & CI/CD", "category": "DevOps", "experienceYears": 3, "evidence": "Ejecución de pruebas headless en contenedores Docker dentro del pipeline", "confidence": 0.92},
            {"name": "JavaScript / TypeScript", "normalizedName": "TypeScript", "category": "Frontend", "experienceYears": 4, "evidence": "Desarrollo de librerías de assertions personalizadas y utilidades de testing", "confidence": 0.94}
        ],
        "languages": [
            {"name": "Español", "level": "Nativo", "evidence": "Lengua materna"},
            {"name": "Inglés", "level": "B2 Intermedio-Alto", "evidence": "Redacción de planes de prueba y bugs en Jira en inglés"}
        ],
        "education": [
            {"degree": "Ingeniería de Software", "institution": "Universidad APEC (UNAPEC)", "graduationYear": 2021}
        ],
        "certifications": [
            {"name": "ISTQB Certified Tester Foundation Level (CTFL)", "issuer": "ISTQB", "year": 2022}
        ],
        "workExperience": [
            {"role": "QA Automation Engineer", "company": "QualityEdge Technologies", "durationYears": 2, "keyAchievements": ["Incrementó la cobertura de regresión automatizada del 20% al 85% con Playwright", "Redujo el tiempo de ejecución de la suite de pruebas de 4 horas a 25 minutos con paralelización"]},
            {"role": "Junior QA Analyst", "company": "DigiSoft Caribe", "durationYears": 2, "keyAchievements": ["Diseñó matrices de prueba y ejecutó pruebas funcionales y de integración", "Automatizó pruebas de regresión de APIs REST con Postman y Newman"]}
        ],
        "pointsToValidate": [
            "Validar experiencia en pruebas de seguridad (OWASP Top 10) y accesibilidad automatizada",
            "Explorar cómo maneja las pruebas 'flaky' (inestables) en el pipeline de CI"
        ],
        "warnings": []
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '2 hours'
);

INSERT INTO candidate_disc_results ("Id", "CandidateId", dominance, influence, steadiness, conscientiousness, primary_style, "CompletedAtUtc")
VALUES (
    '66666666-6666-6666-6666-444444444444',
    '66666666-6666-6666-6666-111111111111',
    40,
    38,
    70,
    94,
    'C',
    NOW() - INTERVAL '2 hours'
);

INSERT INTO candidate_disc_interpretations ("Id", "CandidateId", "DiscResultId", "InterpretationJson", "Status", "ProviderName", "ModelName", "PromptVersion", "CreatedAtUtc")
VALUES (
    '66666666-6666-6666-6666-555555555555',
    '66666666-6666-6666-6666-111111111111',
    '66666666-6666-6666-6666-444444444444',
    $${
        "primaryStyle": "C",
        "summary": "Perfil altamente riguroso, observador y disciplinado. Su foco natural es la detección temprana de anomalías, la precisión en los reportes de bugs y la construcción de frameworks de prueba estables y repetibles.",
        "strengthsToExplore": [
            "Gran capacidad crítica y ojo agudo para identificar casos borde (edge cases) complejos",
            "Disciplina en la documentación y reproducibilidad de incidencias técnicas",
            "Constancia en el mantenimiento y refactorización de suites de prueba automatizadas"
        ],
        "pointsToExplore": [
            "Flexibilidad ante despliegues urgentes con pruebas exploratorias reducidas",
            "Habilidad para comunicar defectos a los desarrolladores de forma constructiva sin generar fricción"
        ],
        "behavioralQuestionTopics": [
            "Manejo de desacuerdos sobre la severidad de un bug con el equipo de desarrollo",
            "Estrategia para priorizar pruebas cuando el tiempo es limitado"
        ],
        "disclaimer": "Esta síntesis conductual es una herramienta de apoyo para el evaluador."
    }$$::jsonb,
    'Processed',
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '2 hours'
);

INSERT INTO candidate_interview_reports ("Id", "CandidateId", "CvAnalysisId", "DiscInterpretationId", "ReportContentJson", "FileUrl", "Status", "Version", "ProviderName", "ModelName", "PromptVersion", "GeneratedAtUtc", "CreatedAtUtc")
VALUES (
    '66666666-6666-6666-6666-666666666666',
    '66666666-6666-6666-6666-111111111111',
    '66666666-6666-6666-6666-333333333333',
    '66666666-6666-6666-6666-555555555555',
    $${
        "candidateOverview": {
            "name": "David Rangel Soto",
            "currentRole": "QA Automation Engineer",
            "experienceYears": 4,
            "professionalSummary": "Ingeniero QA Automation (SDET) con 4 años de experiencia diseñando frameworks de pruebas automatizadas E2E con Playwright y Cypress, pruebas de rendimiento con k6 y testing de APIs REST en pipelines de CI/CD."
        },
        "professionalProfile": {
            "mainSkills": ["Playwright & Cypress", "API Testing", "k6 Load Testing", "Docker & CI/CD", "TypeScript"],
            "relevantExperience": [
                "QA Automation Engineer en QualityEdge Technologies (2 años)",
                "Junior QA Analyst en DigiSoft Caribe (2 años)"
            ],
            "education": ["Ingeniería de Software - UNAPEC"],
            "languages": ["Español (Nativo)", "Inglés (B2 Intermedio-Alto)"],
            "certifications": ["ISTQB CTFL"]
        },
        "discSummary": {
            "primaryStyle": "C",
            "summary": "Perfil metódico, observador y riguroso. Excelente capacidad para detectar casos borde y mantener suites de prueba de alta confiabilidad.",
            "strengthsToExplore": [
                "Identificación proactiva de riesgos de calidad y casos de prueba complejos",
                "Estructura y reproducibilidad en reportes de pruebas"
            ],
            "pointsToExplore": [
                "Balance entre exhaustividad de pruebas y ritmo de entrega ágil"
            ]
        },
        "validationPoints": [
            {
                "topic": "Gestión de Flaky Tests",
                "reason": "Explicar su enfoque técnico para eliminar pruebas inestables en el pipeline de integración continua.",
                "source": "CV"
            },
            {
                "topic": "Pruebas de Carga",
                "reason": "Detallar el diseño de escenarios de prueba de rendimiento con k6 para 5,000 usuarios concurrentes.",
                "source": "CV"
            }
        ],
        "interviewGuide": {
            "professionalQuestions": [
                "¿Cómo decides qué casos de prueba deben automatizarse a nivel de API vs. a nivel de interfaz de usuario E2E?",
                "¿Qué patrón de diseño utilizas para hacer que tu framework en Playwright sea mantenible ante cambios frecuentes en el DOM?"
            ],
            "technicalQuestions": [
                "¿Cómo aíslas los datos de prueba (Test Data Management) para que los tests puedan correr en paralelo sin colisionar en la base de datos?",
                "¿Cómo configuras un script en k6 para simular ramp-up, steady-state y ramp-down de usuarios, y qué métricas analizas (TTFB, p95, error rate)?",
                "¿Qué técnicas aplicas en Playwright para esperar condiciones asíncronas sin utilizar waits estáticos (hardcoded sleeps)?"
            ],
            "behavioralQuestions": [
                "Cuéntanos sobre una situación donde un desarrollador insistía en que un bug que reportaste no era un error del sistema. ¿Cómo demostraste el impacto y cómo llegaron a una resolución? (Metodología STAR).",
                "Describe una ocasión en la que la suite de pruebas automatizadas falló en producción dejando pasar un defecto. ¿Qué aprendiste y cómo ajustaste tu estrategia de calidad?"
            ]
        },
        "disclaimer": "Este informe sirve como herramienta de apoyo para la entrevista y no reemplaza el criterio profesional del evaluador.",
        "fileUrl": null
    }$$::jsonb,
    NULL,
    'Generated',
    1,
    'GoogleGemini',
    'gemini-1.5-flash',
    'v1.0',
    NOW() - INTERVAL '2 hours',
    NOW() - INTERVAL '2 hours'
);
-- =========================================================================================
-- POBLADO INICIAL DEL MÓDULO DE EVALUACIONES PSICOMÉTRICAS GENÉRICAS
-- Sincroniza las evaluaciones de los 6 candidatos hacia candidate_assessments y candidate_assessment_interpretations
-- =========================================================================================
INSERT INTO candidate_assessments ("Id", "CandidateId", assessment_type, scores_assessment_type, primary_style, dimensions_json, completed_at_utc)
SELECT 
    d."Id",
    d."CandidateId",
    'DISC',
    'DISC',
    COALESCE(d.primary_style, 'D/C'),
    jsonb_build_object(
        'Dominance', d.dominance,
        'Influence', d.influence,
        'Steadiness', d.steadiness,
        'Conscientiousness', d.conscientiousness
    ),
    d."CompletedAtUtc"
FROM candidate_disc_results d
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO candidate_assessment_interpretations (
    "Id", "CandidateId", "AssessmentId", assessment_type, interpretation_json, 
    status, provider_name, model_name, prompt_version, created_at_utc, updated_at_utc
)
SELECT 
    i."Id",
    i."CandidateId",
    i."DiscResultId",
    'DISC',
    i."InterpretationJson",
    i."Status",
    i."ProviderName",
    i."ModelName",
    i."PromptVersion",
    i."CreatedAtUtc",
    i."CreatedAtUtc"
FROM candidate_disc_interpretations i
JOIN candidate_assessments ca ON ca."Id" = i."DiscResultId"
ON CONFLICT ("Id") DO NOTHING;
