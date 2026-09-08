# Infraestructura y Despliegue

Este directorio almacena recursos y directrices operativas para los servicios contenerizados del sistema TalentIQ ATS orquestados mediante **Docker Compose**.

> [!NOTE]
> **Aviso de Prototipo Funcional (Infraestructura)**
> La topologia de contenedores en Docker Compose aqui expuesta esta disenada para entornos de desarrollo local, pruebas y validacion de concepto (PoC). No refleja una infraestructura definitiva de alta disponibilidad o produccion empresarial (como Kubernetes, clusters de base de datos administrados o balanceadores de carga en la nube), pero su diseno modular facilita su migracion y adaptacion a cualquier plataforma en la nube o esquema on-premise corporativo.

---

## 1. Servicios Gestionados

| Servicio | Contenedor | Puerto Host | Puerto Interno | Proposito |
| :--- | :--- | :--- | :--- | :--- |
| **Frontend Web** | `ats_frontend` | `5173` | `80` | Consola SPA en React 19 servida por Nginx Alpine |
| **Backend REST API** | `ats_backend` | `5027` | `8080` | API en ASP.NET Core 10 con Clean Architecture |
| **Identity & Access** | `ats_keycloak` | `8085` | `8080` | Keycloak 26.2 (IAM, OIDC PKCE y tema responsivo) |
| **Base de Datos** | `ats_postgres` | `5433` | `5432` | PostgreSQL 16 Alpine con esquemas relacionales e indices |
| **Observabilidad** | `ats_seq` | `8080` / `5341` | `80` / `5341` | Panel de consulta e ingesta de telemetria Serilog |
| **Automatizacion** | `ats_n8n` | `5678` | `5678` | Orquestador de flujos asincronos y batch |

---

## 2. Redes y Volumenes de Persistencia

### 2.1 Red Aislada (`ats_network`)
Todos los contenedores se comunican a traves de una red tipo bridge propia (`ats_network`), permitiendo la resolucion de nombres de servicio interna (e.g. `postgres:5432`, `backend:8080`, `keycloak:8080`, `seq:5341`, `n8n:5678`) sin exponer puertos innecesarios al trafico exterior del host.

### 2.2 Volumenes y Montajes
- `postgres_data`: Persistencia de datos transaccionales de PostgreSQL.
- `seq_data`: Persistencia de eventos y consultas de telemetria de Seq.
- `n8n_data`: Configuracion, credenciales y estado de ejecucion de flujos n8n.
- `backend_storage`: Directorio de almacenamiento fisico de archivos PDF procesados.
- `./keycloak/realm-export.json`: Montaje de solo lectura (`:ro`) para el aprovisionamiento automatico del realm `ats-realm`.
- `./keycloak/themes`: Montaje de solo lectura (`:ro`) para el tema visual corporativo responsivo `talentiq`.

---

## 3. Variables de Entorno Criticas

El archivo `.env` o el entorno del host debe suministrar las siguientes variables:

```bash
# Integracion con Google Gemini AI
Gemini__ApiKey="AIzaSy..."
Gemini__Model="gemini-flash-lite-latest"

# Almacenamiento Desacoplado (Local / S3)
Storage__Provider="Local"
Storage__BasePath="/app/Storage"

# Seguridad y Webhooks
Webhooks__Secret="ats_webhook_secret_2026"
Ingestion__ApiKey="ats_internal_dev_key_2026"

# Base de Datos PostgreSQL
POSTGRES_USER="postgres"
POSTGRES_PASSWORD="tu_password_seguro"
POSTGRES_DB="ats_db"
POSTGRES_PORT=5433

# Credenciales Maestras de Keycloak (primer arranque)
KC_BOOTSTRAP_ADMIN_USERNAME="admin"
KC_BOOTSTRAP_ADMIN_PASSWORD="tu_password_admin"
```

---

## 4. Comandos Operativos de Mantenimiento

```bash
# Iniciar la totalidad de los servicios en segundo plano
docker compose up -d

# Recompilar y reiniciar backend y frontend tras cambios de codigo
docker compose build backend frontend && docker compose up -d backend frontend

# Consultar el estado y salud de los contenedores
docker compose ps

# Visualizar logs en tiempo real de un servicio
docker compose logs -f [backend|frontend|keycloak|postgres|seq|n8n]

# Detener los servicios conservando todos los volumenes de datos
docker compose down

# Detener y purgar volumenes (reinicio total de base de datos)
docker compose down -v
```

