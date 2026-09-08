# MVP: Sistema de Evaluacion y Pre-Entrevista ATS TalentIQ

> [!NOTE]
> **Naturaleza del Documento y Alcance de Prototipo (MVP / PoC)**
> Este documento especifica un Producto Minimo Viable (MVP) concebido como prototipo funcional de exploracion arquitectonica. La secuencia de pasos, cuestionarios y criterios de evaluacion aqui contemplados no sustituyen ni reflejan de forma definitiva los procesos de seleccion reales de una entidad contratante. El diseno del sistema es deliberadamente desacoplado y flexible, lo que permite adaptar, reconfigurar o sustituir etapas segun los flujos de trabajo corporativos vigentes.

## 1. Descripcion General

El MVP tiene como objetivo automatizar la gestion de vacantes y la evaluacion tecnica y conductual de candidatos para generar de forma asistida un informe preentrevista ejecutivo de dos paginas, articulando:

- El curriculum vitae (CV) en formato PDF cargado directamente por el evaluador o postulante.
- El catalogo formal de vacantes y requerimientos tecnicos del puesto (`Job Positions`).
- El resultado oficial de la evaluacion conductual basada en metodologia DISC con clasificacion de estilo primario.
- Google Gemini AI como motor de inteligencia artificial (modelo `gemini-flash-lite-latest` con soporte multi-modelo de contingencia) con blindaje contra inyeccion de prompts.
- Keycloak 26 como servidor de identidad y autorizacion basado en roles (RBAC) con tema responsivo personalizado.
- PostgreSQL 16 como base de datos relacional y transaccional con integridad referencial.
- ASP.NET Core 10 como backend bajo Clean Architecture y CQRS.
- React 19 y Tailwind CSS como interfaz de usuario ejecutiva adaptada a moviles, tabletas y escritorio.

El informe y la guia de preguntas situacionales STAR estan a disposicion del evaluador antes de la entrevista tecnica.

---

## 2. Objetivo del MVP

Cuando se crea una vacante y se carga el CV de un postulante junto a sus puntajes del test DISC, el sistema debe estructurar automaticamente un expediente con diagnostico de competencias, perfil conductual y guia de preguntas de entrevista estructuradas, permitiendo al evaluador tomar una decision informada y emitir su dictamen oficial.

---

## 3. Entradas del Sistema

1. **Puesto / Vacante Objetivo:** Seleccion de una vacante formal existente (`job_position_id`) o cargo personalizado con sus requerimientos tecnicos.
2. **Datos del Candidato:** Nombre, apellido, correo electronico y telefono.
3. **Documento Curricular:** Archivo PDF de maximo 15 MB con verificacion de integridad.
4. **Texto Extraido:** Contenido textual procesado de forma segura en memoria por `PdfPigTextExtractor`.
5. **Resultados DISC:** Puntajes numericos de Dominancia (D), Influencia (I), Estabilidad (S) y Cumplimiento (C).

---

## 4. Salida Principal

El resultado consolidado del proceso es el **Expediente e Informe Preentrevista de Dos Paginas**, compuesto por:

1. **Resumen Profesional:** Sintesis del perfil y trayectoria adaptada al puesto.
2. **Competencias Tecnicas y Evidencias:** Habilidades normalizadas con su respectiva cita textual verificada (*Grounding Check*).
3. **Trayectoria y Formacion:** Experiencia laboral previa, educacion, certificaciones e idiomas.
4. **Sintesis Conductual DISC:** Descriptores de desempeno laboral y fortalezas clave con grafico de radar hexagonal SVG.
5. **Puntos a Validar:** Aspectos o brechas que requieren aclaracion durante la entrevista.
6. **Guia de Preguntas STAR:**
   - Preguntas profesionales sobre decisiones de carrera y proyectos.
   - Preguntas tecnicas contextualizadas a sus herramientas.
   - Preguntas conductuales orientadas a situaciones de cambio o trabajo bajo presion.
7. **Resolucion Oficial y Minuta:** Registro del dictamen del evaluador (Aprobado, En Reserva o No Seleccionado) con notas confidenciales de auditoria.
8. **Documento Oficial PDF:** Descarga del informe formateado para comite de seleccion.

---

## 5. Alcance Funcional Implementado

### 5.1 Funcionalidades Completadas
1. **Modulo de Gestion de Vacantes:** Alta, modificacion de estado y conteo en tiempo real de candidatos vinculados por puesto.
2. **Ingesta Directa de CV con IA:** Modal con zona drag-and-drop, sliders interactivos DISC y seguimiento visual del pipeline en tiempo real.
3. **Analisis Curricular con Gemini AI:** Extraccion estructurada bajo JSON Schema estricto.
4. **Interpretacion Conductual DISC:** Generacion de descriptores laborales a partir de los puntajes D, I, S, C.
5. **Generacion Automatizada de Guias STAR:** Preguntas situacionales personalizadas para el perfil.
6. **Persistencia Relacional Transaccional:** Esquema en PostgreSQL 16 con llaves foraneas indexadas (`01` al `06-create-job-positions.sql`).
7. **Consola Web Responsiva:** Adaptabilidad total en smartphones, tabletas y monitores de escritorio (Navbar con menu hamburguesa, modals fluidos `h-[95vh]`, RadarChart con SVG `viewBox`).
8. **Identidad Centralizada y RBAC con Keycloak 26:** Flujo PKCE en React, validacion JWKS en ASP.NET Core y tema visual corporativo responsivo.
9. **Delegacion de Candidatos:** Asignacion individual de expedientes de administradores hacia evaluadores responsables.
10. **Seguridad Defensiva contra Prompt Injection:** Cuatro capas de defensa (`CvSecuritySanitizer`, `systemInstruction`, Grounding Check y Human-in-the-Loop).
11. **Hardening de Seguridad Backend:** Prevencion de Path Traversal en almacenamiento de archivos, validacion HMAC timing-safe en webhooks y middleware global de excepciones RFC 7807.
12. **Desacoplamiento y Modularidad Arquitectonica:** Abstraccion `IAiProvider` con conmutacion automatica a `MockAiProvider` ante ausencia de credenciales; abstraccion `IStorageService` con soporte local y S3; capa de cache en memoria `ICacheService`.
13. **Procesamiento en Segundo Plano (Background Channels):** Cola desacoplada `IBackgroundJobQueue` y `QueuedHostedService` para absorcion de cargas asincronas de evaluacion curricular.
14. **Migraciones Formales EF Core:** Modelo declarativo en C# con ejecucion automatica `db.Database.Migrate()` en el arranque.
15. **Suite Completa de Pruebas Automatizadas:** 69 pruebas integradas en `Ats.slnx` cubriendo invariantes de dominio, handlers de aplicacion, reglas arquitectonicas y casos de seguridad.
16. **Modularizacion Frontend:** Subcomponentes dedicados de expediente bajo `src/components/evaluator/` y configuracion desacoplada via `VITE_API_BASE_URL`.

### 5.2 Funcionalidades para Siguientes Fases (Hoja de Ruta)
1. Notificaciones asincronas en tiempo real por WebSockets (SignalR) ante nuevas asignaciones.
2. Portal web desacoplado de auto-postulacion directa para candidatos con captcha empresarial.
3. Integracion con calendarios corporativos (Google Calendar y Microsoft Outlook / Teams).
4. Analitica avanzada de pipeline de seleccion y embudos de contratacion por departamento.

---

## 6. Flujo de Procesamiento y Seguridad

```text
[ Carga de CV en PDF + Vacante + Puntajes DISC ]
                      │
                      ▼
[ Extraccion de Texto PDF en Memoria (PdfPig) ]
                      │
                      ▼
[ Pre-Inspeccion Heuristica (CvSecuritySanitizer) ]
  • Deteccion de patrones de evasion y prompt injection
  • Sanitizacion y escape de etiquetas XML
                      │
                      ▼
[ Gemini AI (systemInstruction + <untrusted_applicant_cv>) ]
  • Extraccion de experiencia, educacion y skills tecnicas
                      │
                      ▼
[ Evidence Grounding Check en C# ]
  • Validacion de citas textuales exactas en el CV
                      │
                      ▼
[ Evaluacion Conductual DISC & Preguntas STAR ]
                      │
                      ▼
[ Expediente Consolidado en Dashboard Responsivo ]
                      │
                      ▼
[ Evaluador Humano Emite Dictamen Oficial y Notas ]
```

---

## 7. Responsabilidades de Componentes

### 7.1 Backend (ASP.NET Core 10)
- Exponer API REST documentada con Swagger/OpenAPI.
- Validar tokens JWT emitidos por Keycloak y exigir politicas de autorizacion basadas en roles.
- Ejecutar la sanitizacion previa contra inyeccion de prompts y coordinar las llamadas seguras a Gemini AI.
- Validar la consistencia de las evidencias extraidas del CV.
- Gestionar la persistencia transaccional con Entity Framework Core y llaves foraneas.
- Proveer proteccion contra Path Traversal y ataques de canal lateral en webhooks.

### 7.2 Servidor de Identidad (Keycloak 26)
- Centralizar la administracion de usuarios, credenciales y roles corporativos (`ats_admin`, `ats_recruiter`).
- Servir la interfaz de login corporativa responsiva mediante el tema `talentiq`.
- Emitir tokens de acceso seguros y gestionar sesiones con PKCE.

### 7.3 Frontend (React 19 + TypeScript)
- Proveer la experiencia de usuario ejecutiva para administradores y reclutadores.
- Renderizar de forma reactiva el dashboard, el grafico de radar conductual DISC y el expediente de 5 pestañas.
- Garantizar soporte tactil ergonomico y visualizacion adaptada a cualquier resolucion.
- Alertar visualmente al evaluador ante anomalias de integridad detectadas en los documentos.