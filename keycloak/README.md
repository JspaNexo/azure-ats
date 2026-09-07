# Servidor de Identidad y Accesos Keycloak 26 (IAM / RBAC)

Este directorio contiene los archivos de configuracion, exportacion de realm y temas visuales para el servidor de identidad **Keycloak 26.2** de TalentIQ ATS.

> [!NOTE]
> **Aviso de Prototipo (Seguridad e Identidad)**
> Los roles (`ats_admin`, `ats_recruiter`), usuarios de demostración y políticas de sesión aquí provistos son parte de un prototipo para ilustrar el control de acceso basado en roles (RBAC). El sistema puede integrarse con directorios corporativos reales (Active Directory / LDAP / Microsoft Entra ID / Google Workspace) o adaptar su matriz de permisos y roles a la estructura organizativa de la empresa.

---

## 1. Estructura del Directorio

```text
keycloak/
├── realm-export.json          # Definicion exportable del realm ats-realm, clientes, roles y cuentas
├── themes/                    # Temas personalizados de interfaz de Keycloak
│   └── talentiq/              # Tema corporativo responsivo de TalentIQ ATS
│       └── login/             # Plantillas Freemarker (.ftl), estilos CSS y assets
│           ├── theme.properties
│           ├── login.ftl
│           └── resources/css/styles.css
└── README.md                  # Esta documentacion
```

---

## 2. Realm Corporativo (`ats-realm`)

Keycloak opera con un realm dedicado denominado `ats-realm`, el cual aisla completamente la seguridad del sistema ATS del realm `master`.

### 2.1 Clientes Configurados

1. **`ats-frontend` (Cliente Publico):**
   - Protocolo: OpenID Connect.
   - Access Type: Public.
   - Standard Flow Enabled: Si (con autorizacion PKCE - Proof Key for Code Exchange).
   - Valid Redirect URIs: `http://localhost:5173/*`, `http://127.0.0.1:5173/*`.
   - Web Origins: `+`, `http://localhost:5173`.

2. **`ats-backend` (Audiencia de API):**
   - Audiencia de token validada por el middleware de autenticacion JWT Bearer de ASP.NET Core.

### 2.2 Roles de Usuario (RBAC)

- **`ats_admin` (Administrador):**
  - Acceso irrestricto a la totalidad del sistema.
  - Creacion y modificacion de vacantes de empleo.
  - Asignacion y delegacion de postulantes a evaluadores.
  - Carga directa de postulantes y evaluacion con IA.
  - Emision de dictamenes oficiales.

- **`ats_recruiter` (Reclutador / Evaluador):**
  - Acceso al panel de postulantes enfocado en "Mis Asignados".
  - Carga directa de CVs y evaluacion conductual asistida por IA.
  - Consulta del expediente, revision de preguntas STAR y emision del dictamen de entrevista.

### 2.3 Cuentas Preconfiguradas para Pruebas

| Usuario | Contraseña | Rol | Nombre Completo | Correo Electronico |
| :--- | :--- | :--- | :--- | :--- |
| `admin` | `Admin123!` | `ats_admin` | Administrador Comite | `admin@ats.com` |
| `carlos.mendoza` | `Recruiter123!` | `ats_recruiter` | Carlos Mendoza | `carlos.mendoza@empresa.com` |
| `laura.sanchez` | `Recruiter123!` | `ats_recruiter` | Laura Sanchez | `laura.sanchez@empresa.com` |

---

## 3. Tema Corporativo Responsivo (`themes/talentiq`)

Keycloak utiliza por defecto una plantilla estandar que presenta desproporciones y fallos de ajuste en dispositivos moviles. Para ofrecer una experiencia corporativa homogenea, se desarrollo el tema `talentiq`:

- **Diseno Adaptativo (Mobile-First):** Contenedor central fluido con anchos maximos controlados (`max-w-md`), margenes tactiles y botones optimizados para pantallas tactiles.
- **Identidad Visual:** Paleta de colores consistente con la consola web en tonos azul y pizarra corporativos (`brand-500`, `slate-900`).
- **Seguridad en Entrada:** Formulario con etiquetas claras, feedback de errores visuales y soporte para gestion de contraseñas.

