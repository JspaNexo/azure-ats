# Frontend ATS TalentIQ - Consola Ejecutiva de Seleccion Responsiva

Modulo de interfaz de usuario web para el sistema de seleccion de talento TalentIQ ATS. Construido con **React 19**, **TypeScript**, **Vite** y **Tailwind CSS**, diseñado para una navegacion fluida y adaptativa en dispositivos moviles, tabletas y computadoras de escritorio, integrando autenticacion **Keycloak 26 (IAM)** y comunicacion con la API REST en **ASP.NET Core 10**.

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
4. **Expediente Integral de 5 Pestañas (`EvaluatorReviewModal`):**
   - *1. CV y Perfil:* Resumen profesional, experiencia laboral previa, formacion academica, competencias tecnicas y alertas de integridad curricular.
   - *2. Perfil DISC:* Grafico Radar hexagonal interactivo, descriptores conductuales y fortalezas clave.
   - *3. Preguntas STAR:* Guia situacional de indagacion profesional, tecnica y conductual.
   - *4. Dictamen:* Formulario de resolucion oficial (Aprobado, En Reserva, Descartado) con notas confidenciales.
   - *5. Expediente PDF:* Previsualizacion y descarga directa del documento ejecutivo oficial.

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
│   │   ├── EvaluatorReviewModal.tsx       # Expediente integral del candidato de 5 pestañas
│   │   ├── UploadCandidateModal.tsx       # Carga directa de CV, sliders DISC y evaluacion IA en vivo
│   │   ├── CreateJobPositionModal.tsx     # Formulario de alta de vacantes formales
│   │   ├── AssignRecruiterModal.tsx       # Modal de delegacion de candidatos a evaluadores
│   │   └── RadarChart.tsx                 # Grafico de radar SVG escalable y responsivo
│   ├── context/
│   │   └── AuthContext.tsx                # Contexto de sesion Keycloak (PKCE, tokens y roles)
│   ├── services/
│   │   ├── api.ts                         # Cliente HTTP tipado con fetch e inyeccion de token Bearer
│   │   └── keycloak.ts                    # Configuracion del cliente OIDC Keycloak JS
│   ├── types/
│   │   └── index.ts                       # Modelos TypeScript (Candidate, JobPosition, etc.)
│   ├── index.css                          # Configuracion de estilos Tailwind CSS
│   └── main.tsx                           # Punto de entrada de la aplicacion React
├── Dockerfile                             # Multi-stage build con Node.js 22 Alpine y Nginx Alpine
├── nginx.conf                             # Configuracion Nginx con proxy inverso hacia backend
├── package.json                           # Dependencias y comandos npm
└── tsconfig.json                          # Configuracion de compilacion TypeScript estricta
```

---

## 3. Integracion con Keycloak IAM

El frontend utiliza el patron de flujo PKCE de OpenID Connect a traves de la libreria oficial `keycloak-js`:

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

