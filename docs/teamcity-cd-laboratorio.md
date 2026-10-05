# CI/CD con TeamCity, GHCR y Docker en Windows

El laboratorio despliega la aplicación en tu PC. El flujo queda así:

```text
commit + push a main
  → TeamCity: compilar y ejecutar pruebas
  → construir imágenes e integración HTTP en contenedores temporales
  → publicar las mismas imágenes en GHCR
  → descargar y desplegar por digest en Docker Desktop
  → comprobar web, base de datos, autenticación y API
  → conservar la versión nueva o intentar recuperar la anterior si falla
```

Las ramas diferentes de `main` ejecutan CI pero omiten la publicación y el despliegue. Un fallo de compilación o pruebas detiene el flujo antes de publicar. `ATS_ENABLE_CD=false` permite seguir usando CI mientras configuras GHCR.

## 1. Preparar GitHub Container Registry (GHCR)

Se utilizarán `ghcr.io/jspanexo/ats-backend:tc-ID` y `ghcr.io/jspanexo/ats-frontend:tc-ID`, tomando el propietario del repositorio `JspaNexo/azure-ats`. Si publicarás en otra cuenta, cambia `ATS_GHCR_OWNER` y `ATS_GHCR_USER` en el YAML.

En GitHub abre **Settings → Developer settings → Personal access tokens → Tokens (classic)**. Crea un token con `write:packages` y `read:packages`, nombre `teamcity-ats-lab` y una fecha de vencimiento. También puedes abrir [el formulario con write:packages seleccionado](https://github.com/settings/tokens/new?scopes=write:packages). No hace falta `delete:packages` para este flujo. Guarda el token en TeamCity como se indica a continuación; no lo escribas en el repositorio ni lo pegues en el chat.

No necesitas crear manualmente los paquetes ni instalar un servidor de registro. El primer `docker push` creará los paquetes, inicialmente privados, en tu cuenta de GitHub. Puedes verlos en **Tu perfil → Packages** y vincularlos con `azure-ats` desde la configuración del paquete. Consulta la [documentación oficial de GHCR](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry).

## 2. Configurar el secreto y activar CD en TeamCity

En la configuración del **proyecto** que contiene tu pipeline, abre **Parameters → Add new parameter** y crea:

| Campo | Valor |
| --- | --- |
| Nombre | `env.ATS_GHCR_TOKEN` |
| Tipo de parámetro | Environment variable |
| Tipo de valor | Password |
| Valor | El token de GitHub |

El parámetro debe estar disponible para el job. El script lo lee desde el entorno; el YAML no contiene el token ni una referencia que impida ejecutar CI antes de configurarlo. TeamCity documenta los tipos de parámetros en [Configuring Build Parameters](https://www.jetbrains.com/help/teamcity/configuring-build-parameters.html).

Los otros valores ya están en [teamcity-pipelines.yml](../teamcity-pipelines.yml):

| Parámetro | Valor del laboratorio |
| --- | --- |
| `env.ATS_GHCR_USER` | `JspaNexo`, usuario dueño del token |
| `env.ATS_GHCR_OWNER` | `jspanexo`, namespace de las imágenes en minúsculas |
| `env.ATS_LAB_DIR` | `C:/Users/jspaniagua/ats-lab`, carpeta persistente fuera del checkout |
| `env.ATS_PUBLIC_KEYCLOAK_URL` | `http://localhost:18085` |
| `env.ATS_ENABLE_CD` | Cambiar de `'false'` a `'true'` después de guardar el secreto |

En el editor visual de tu pipeline copia el YAML actualizado en **Settings → YAML** y guarda. Confirma que el repositorio principal tenga `main` como rama predeterminada y que siga activo **Auto-Run Pipeline → On new changes**. La configuración visual de disparadores no se importa desde este archivo.

El agente debe seguir conectado y Docker Desktop debe estar iniciado con contenedores Linux. El usuario del agente necesita poder escribir en `ATS_LAB_DIR`, acceder a Docker y conectarse a GHCR. Los puertos 15173 y 18085 deben estar disponibles.

El YAML selecciona explícitamente **My agent**, instalado en `C:/Users/jspaniagua/buildAgent`, para ejecutar y desplegar en la PC del laboratorio. Otro agente Windows puede no tener Docker en su PATH o desplegar en un destino diferente. Si cambias el nombre del agente, actualiza el requisito `teamcity.agent.name` en el YAML. Antes de ejecutar, confirma que **My agent** esté conectado, autorizado y habilitado en TeamCity.

El commit se obtiene con `git rev-parse --verify HEAD` después del checkout. No definas `env.BUILD_SOURCEVERSION` con `%system.build.vcs.number%` ni `%build.vcs.number%`: una referencia no disponible puede impedir que TeamCity asigne el job al agente. Si ese parámetro quedó guardado en el proyecto, pipeline o job, elimínalo de esa configuración. Git debe estar instalado y el checkout debe hacerse en el agente conservando `.git`.

Haz commit y push de estos archivos y ejecuta el pipeline sobre `main`. En el primer intento puedes usar **Run** para ver el proceso completo sin esperar otro cambio. No se publica ni se despliega desde otras ramas aunque CD esté habilitado.

## 3. Entrar a la aplicación

### Contextos de construcción de Docker

Cada Dockerfile utiliza la carpeta de su servicio como contexto. Dentro de `backend` funciona `docker build -f Dockerfile .`, igual que el paso Docker de Build en TeamCity. Dentro de `frontend` funciona el mismo comando. Desde la raíz del repositorio utiliza `docker build -f backend/Dockerfile backend` y `docker build -f frontend/Dockerfile frontend`. Las instrucciones `COPY` son relativas a esas carpetas.

El YAML de este repositorio construye las imágenes dentro del paso **Construir imagenes y probar integracion**, usando `automation/compose.ci.yml` con estos mismos contextos. `docker-compose.yml` también utiliza las carpetas de cada servicio. Los cambios de Dockerfiles y Compose deben estar incluidos en el commit enviado al repositorio para que Build los utilice.

Para un paso Docker independiente con rutas relativas al checkout, configura el Dockerfile como `backend/Dockerfile` y el contexto como `backend`. Para el frontend, utiliza `frontend/Dockerfile` y el contexto `frontend`. Si el runner trabaja directamente dentro de la carpeta del servicio, puede usar `Dockerfile` y `.`.

Después de un despliegue verificado:

- Aplicación: <http://localhost:15173>.
- Salud de API y base de datos: <http://localhost:15173/health>.
- Keycloak: <http://localhost:18085>.
- Usuario de la aplicación: `admin`, contraseña `Admin123!`.
- Reclutador de ejemplo: `carlos.mendoza`, contraseña `Recruiter123!`.

Estos usuarios se importan desde el realm del proyecto al crear la base por primera vez. La contraseña del administrador de la consola de Keycloak es diferente: el script la genera al iniciar el laboratorio y la guarda en `ATS_LAB_DIR/credentials.env`. Si cambias la contraseña del usuario `admin` de la aplicación, agrega `env.ATS_LAB_ADMIN_PASSWORD` como **Password** en TeamCity para que la verificación de despliegue use el valor nuevo.

El laboratorio utiliza IA simulada, igual que la integración. Las comprobaciones posteriores al despliegue consultan datos existentes y comprueban el login; no crean candidatos ni ejecutan la ingesta de CV en la base persistente.

## 4. Qué se conserva y qué pasa si falla

El proyecto Docker `ats-lab` conserva la base en `ats-lab_postgres_data` y los archivos en `ats-lab_backend_storage`. No usa los contenedores del `docker-compose.yml` de desarrollo. CI utiliza proyectos aleatorios `ats-ci-*` y elimina sus datos temporales al terminar.

Cada release guarda su Compose, SQL de inicialización, realm y tema en `ATS_LAB_DIR/releases/tc-ID`. `current.json` registra la última versión verificada; `previous.json` conserva la referencia anterior. Las imágenes se descargan por digest y se comprueba que coincidan con los IDs registrados por las pruebas. No se reconstruyen durante el despliegue. Los resultados se publican en **Artifacts → ci-results/deployment.json**.

Si falla la publicación, no se modifica el laboratorio. Si falla el arranque o la verificación y existe una versión anterior, el script vuelve a aplicar sus imágenes y comprueba que funcione. El pipeline permanece rojo para que revises la causa. En el primer despliegue no hay una versión anterior disponible; una recuperación también puede fallar y el reporte lo indica.

La recuperación de imágenes **no restaura la base de datos**. Los SQL de `database/init` se aplican solo al crear el volumen PostgreSQL vacío; agregar un SQL al repositorio no migra automáticamente un laboratorio existente. Para cambios de esquema posteriores habrá que incorporar migraciones compatibles y respaldos antes de desplegar.

Si se cancela el proceso abruptamente, puede quedar la carpeta vacía `ATS_LAB_DIR/.deployment-lock`. Confirma que ningún despliegue siga ejecutándose antes de eliminar únicamente esa carpeta vacía. No elimines los volúmenes para resolver errores de publicación o arranque.

Este destino usa Keycloak en modo desarrollo, cuentas de ejemplo y puertos accesibles solamente desde la PC. Un despliegue público requiere configurar HTTPS, credenciales, migraciones y respaldos adecuados a ese entorno.

## 5. Detener o reiniciar el laboratorio

Desde PowerShell, utiliza la release guardada, sin borrar datos:

```powershell
$atsLabDir = 'C:\Users\jspaniagua\ats-lab'
$atsRelease = Get-Content -LiteralPath "$atsLabDir\current.json" -Raw | ConvertFrom-Json
$env:ATS_BACKEND_IMAGE = $atsRelease.backend
$env:ATS_FRONTEND_IMAGE = $atsRelease.frontend

# Detener; conserva los volúmenes.
docker compose --project-name ats-lab --file "$($atsRelease.directory)\compose.yml" --env-file "$atsLabDir\credentials.env" stop

# Iniciar de nuevo usando las imágenes ya descargadas.
docker compose --project-name ats-lab --file "$($atsRelease.directory)\compose.yml" --env-file "$atsLabDir\credentials.env" up -d --no-build
```

Para comprobar únicamente la lógica de selección de rama y recuperación sin Docker:

```powershell
node --test automation/tests/lab-release.test.mjs
```

La verificación completa de despliegue y recuperación puede ejecutarse después de `ci-stack.mjs`, con `ATS_PUBLIC_KEYCLOAK_URL=http://localhost:18085` y un tag `tc-NUMERO`. Detén antes el laboratorio si ocupa 15173 o 18085. Ejecuta `node automation/tests/lab-docker.mjs`: crea un proyecto independiente `ats-lab-verification-*`, verifica el arranque, provoca un fallo y comprueba la recuperación sin reemplazar los volúmenes. Finalmente elimina únicamente los contenedores, red y volúmenes de ese proyecto de prueba. Sus archivos quedan en `scratch/`, ignorado por Git.
