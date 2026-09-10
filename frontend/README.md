# Frontend ATS TalentIQ - Consola Ejecutiva de Seleccion Responsiva

Modulo de interfaz de usuario web para el sistema de seleccion de talento TalentIQ ATS. Construido con **React 19**, **TypeScript**, **Vite** y **Tailwind CSS**, diseñado para una navegacion fluida y adaptativa en dispositivos moviles, tabletas y computadoras de escritorio, integrando autenticacion **Keycloak 26 (IAM)** y comunicacion con la API REST en **ASP.NET Core 10**.

> [!NOTE]
> **Aviso de Prototipo Funcional (Frontend)**
> Esta interfaz representa un prototipo de consola ejecutiva para validar la experiencia de usuario (UX) en la revision curricular y psicometrica. La disposicion de controles, modales de captura y formularios de evaluacion no corresponden de forma rigida a un proceso organizacional inmutable; sus componentes React modulares y estilos Tailwind estan preparados para adaptarse, personalizarse o reconfigurarse segun el manual de marca y las fases de contratacion de cada institucion.

---

## 1. Caracteristicas Principales

### 1.1 Consola Orientada a Roles (RBAC)
- **Panel de Administrador (`ats_admin`):**
  - Supervision global del pipeline de seleccion y metricas consolidadas.
  - Registro de puestos y vacantes formales de la organizacion.
  - Asignacion y delegacion de postulantes a evaluadores individuales.
  - Carga directa de CVs con evaluacion IA en tiempo real y emision de dictamenes.
- **Panel de Reclutador / Evaluador (`ats_recruiter`):**
  - Enfoque automatico en expedientes bajo su responsabilidad mediante la vista "Mis Asignados".
  - Carga directa de postulantes y evaluacion automatizada con Gemini AI.
  - Aplicacion guiada de preguntas estructuradas STAR y emision del dictamen oficial con notas confidenciales.

### 1.2 Modales Ejecutivos y Flujos Operativos
1. **Modal de Carga Directa de Candidatos (`UploadCandidateModal`):**
   - Selector de vacante activa vinculada al proceso.
   - Carga de CV en formato PDF con zona drag-and-drop y validacion de formato y tamano (maximo 15 MB).
   - Captura de datos personales del postulante.
   - Controles deslizantes (sliders) interactivos para capturar los puntajes del test conductual DISC (Dominancia, Influencia, Estabilidad, Cumplimiento) con clasificacion automatica del perfil primario.
   - Monitoreo en vivo de las fases del analisis de Gemini: extraccion, seguridad, perfil, DISC y preguntas STAR.
2. **Modal de Creacion de Vacantes (`CreateJobPositionModal`):**
   - Formulario empresarial para registrar titulo, departamento, nivel de seniority, anos minimos de experiencia, requisitos clave y descripcion del puesto. (Acceso exclusivo Administrador).
3. **Modal de Asignacion de Reclutador (`AssignRecruiterModal`):**
   - Selector intuitivo de evaluador responsable (`carlos.mendoza`, `laura.sanchez`) con actualizacion inmediata en el dashboard.
4. **Modal Visor de PDF Original (`PdfViewerModal`):**
   - Visor web interactivo a pantalla completa para examinar el documento PDF original del postulante, con controles de nueva pestaña y descarga voluntaria.
5. **Expediente Integral Modular de 5 Pestañas (`EvaluatorReviewModal`):**
   - *1. Síntesis & CV Original (`CvAnalysisTab`):* Selector interactivo para alternar entre "Síntesis Asistida" y "Ver CV Original (PDF)" en visor embebido. Implementado con retención de Blob URL en `useRef` para permitir alternancia continua e instantánea sin peticiones redundantes ni errores de revocación. Incluye banner ético de supervisión humana (*Human-in-the-Loop*).
   - *2. Perfil Psicométrico (`DiscProfileTab`):* Gráfico Radar hexagonal interactivo, descriptores conductuales, fortalezas y estilo predominante.
   - *3. Guía STAR (`InterviewGuideTab`):* Guía situacional de indagacion estructurada bajo metodología STAR (preguntas profesionales, técnicas y conductuales).
   - *4. Dictamen RRHH (`DecisionTab`):* Formulario oficial de resolución soberana de Recursos Humanos (Aprobado, En Reserva, No Seleccionado) con notas confidenciales.
   - *5. Expediente PDF (`PdfReportTab`):* Previsualización web integrada del informe pre-entrevista y descarga voluntaria en PDF.
6. **Acción Rápida en Dashboard:**
   - Botón directo "Ver CV" en cada tarjeta/fila para abrir el expediente o visor de PDF original en un solo clic.

### 1.3 Diseno Responsivo y Ergonomia Visual (Mobile-First)
- **Barra de Navegacion Adaptativa (`Navbar`):** Menu hamburguesa tactil con animacion suave en pantallas moviles (< 640px) y boton de acceso rapido ("Cargar").
- **Grafico Conductual Fluido (`RadarChart`):** Espacio de coordenadas relativo `viewBox="0 0 300 300"` y ancho dinamico que evita truncamiento de etiquetas perimetrales.
- **Tarjetas y Listados Flexibles:** Distribucion en 1, 2 o 4 columnas segun la resolucion del dispositivo. En movil, el reclutador asignado se visualiza mediante un chip con icono accesible.
- **Modales Adaptativos:** Altura dinamica `h-[95vh] sm:h-auto sm:max-h-[90vh]` con scroll vertical independiente para evitar cortes de contenido en smartphones.

---

## 2. Estructura del Codigo Fuente

```text
frontend/
├── src/
│   ├── components/
│   │   ├── App.tsx                        # Layout principal, control de estado y toasts globales
│   │   ├── Navbar.tsx                     # Barra de navegacion adaptativa con menu hamburguesa
│   │   ├── StatsCards.tsx                 # Cuadricula fluida de metricas (1, 2 o 4 columnas)
│   │   ├── QuickStartGuide.tsx            # Guia rapida para evaluadores con colapso responsivo
│   │   ├── EvaluatorCandidateDashboard.tsx# Buscador, filtros por puesto/estado y tarjetas de candidatos
│   │   ├── EvaluatorReviewModal.tsx       # Contenedor modular del expediente integral
│   │   ├── PdfViewerModal.tsx             # Modal interactivo para visualizacion web de PDF original
│   │   ├── evaluator/                     # Pestanas desacopladas del expediente:
│   │   │   ├── CvAnalysisTab.tsx          # Sintesis asistida, visor PDF original y banner etico
│   │   │   ├── DiscProfileTab.tsx         # Radar SVG y descriptores conductuales
│   │   │   ├── InterviewGuideTab.tsx      # Guia de indagacion situacional STAR
│   │   │   ├── DecisionTab.tsx            # Formulario de dictamen oficial soberano de RRHH
│   │   │   └── PdfReportTab.tsx           # Visor interactivo y descarga de informe ejecutivo PDF
│   │   ├── UploadCandidateModal.tsx       # Carga directa de CV, sliders DISC y generacion asistida en vivo
│   │   ├── CreateJobPositionModal.tsx     # Formulario de alta de vacantes formales
│   │   ├── AssignRecruiterModal.tsx       # Modal de delegacion de candidatos a evaluadores
│   │   └── RadarChart.tsx                 # Grafico de radar SVG escalable y responsivo
│   ├── context/
│   │   └── AuthContext.tsx                # Contexto de sesion Keycloak (PKCE, tokens y roles)
│   ├── services/
│   │   ├── api.ts                         # Cliente HTTP fuertemente tipado (sin tipos any) via VITE_API_BASE_URL
│   │   └── keycloak.ts                    # Configuracion del cliente OIDC Keycloak JS
│   ├── types/
│   │   └── index.ts                       # Modelos TypeScript estrictos (UploadCvResponse, CvDocumentDto, etc.)
│   ├── index.css                          # Configuracion de estilos Tailwind CSS
│   └── main.tsx                           # Punto de entrada de la aplicacion React
├── .env.example                           # Variables de entorno frontend (VITE_API_BASE_URL, etc.)
├── Dockerfile                             # Multi-stage build con Node.js 22 Alpine y Nginx Alpine
├── nginx.conf                             # Configuracion Nginx con proxy inverso hacia backend
├── package.json                           # Dependencias y comandos npm
└── tsconfig.json                          # Configuracion de compilacion TypeScript estricta
```

---

## 3. Integracion con Keycloak IAM y Configuracion de Entorno

El frontend utiliza el patron de flujo PKCE de OpenID Connect a traves de la libreria oficial `keycloak-js`:

- **Variables de Entorno (.env):**
  - `VITE_API_BASE_URL`: URL base de la API backend (por defecto `/api/v1` relativo para Nginx o `http://localhost:5027/api/v1` en desarrollo directo).
  - `VITE_KEYCLOAK_URL`: URL del servidor de identidad (por defecto `http://localhost:8085`).
  - `VITE_KEYCLOAK_REALM`: Realm de autenticacion (`ats-realm`).
  - `VITE_KEYCLOAK_CLIENT_ID`: Identificador de cliente OIDC (`ats-frontend`).

- **URL de Identidad:** `http://localhost:8085`
- **Realm:** `ats-realm`
- **Client ID:** `ats-frontend`
- **Tema Visual Corporativo:** Integrado con el tema personalizado `keycloak/themes/talentiq`, que garantiza una apariencia sobria, profesional y adaptada a moviles desde la misma pantalla de inicio de sesion.
- **Renovacion de Tokens:** Actualizacion automatica del token de acceso cada 60 segundos con manejo de sesion expirada.

---

## 4. Scripts y Comandos de Ejecucion

En el directorio `frontend/`:

```bash
# Instalar dependencias
npm install

# Iniciar servidor de desarrollo con Hot Module Replacement (HMR)
npm run dev

# Compilar verificando tipos TypeScript para produccion
npm run build

# Previsualizar el paquete compilado en dist/
npm run preview
```

