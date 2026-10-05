# Comprobaciones HTTP

La integración completa para Azure DevOps y ejecución local está documentada en [automatizaciones DevOps](../docs/automatizaciones-devops.md). Puede iniciarla con `node automation/scripts/ci-stack.mjs`; crea y limpia sus servicios temporales automáticamente.

## CI con TeamCity en Windows

El archivo [teamcity-pipelines.yml](../teamcity-pipelines.yml) ejecuta compilación y pruebas .NET, construcción del frontend e integración con Docker en un agente Windows propio. Copie su contenido en **Settings → YAML** del pipeline conectado al repositorio y guarde. Los disparadores automáticos se configuran en **Auto-Run Pipeline → On new changes**.

El usuario que ejecuta el agente necesita acceso a .NET SDK 10, Node.js 22 o posterior, Git, Docker con contenedores Linux y Docker Compose v2 o posterior. Si usa Docker Desktop, debe estar iniciado durante la ejecución; compruebe el acceso desde la terminal del agente con `docker version` y `docker compose version`.

La integración se ejecuta después de la validación. Construye las imágenes `ats-ci-backend:tc-ID_BUILD` y `ats-ci-frontend:tc-ID_BUILD`, inicia PostgreSQL, Keycloak, API y Nginx temporales y ejecuta el flujo HTTP con IA simulada. El resumen registra el commit de TeamCity. El script elimina los contenedores, redes y volúmenes temporales al terminar, incluso si falla una comprobación; conserva las imágenes construidas. Si se cancela o se termina el proceso antes de su limpieza, desde el directorio de checkout del agente ejecute `node automation/scripts/ci-stack.mjs cleanup`.

En **Artifacts** se publican los resultados TRX, el bundle del frontend y los archivos de `ci-results/`, incluido `summary.json`, `http.json` y, cuando falla la integración, `compose.log`.

El flujo completo de publicación en GHCR, despliegue persistente en tu PC Windows y recuperación de la versión anterior está en [CI/CD para el laboratorio](../docs/teamcity-cd-laboratorio.md). El YAML incluye el paso de despliegue con `ATS_ENABLE_CD=false`: configura el token como Password en TeamCity y activa CD para utilizarlo. Las ramas diferentes de `main` conservan las comprobaciones de CI sin publicar ni desplegar.

### CI en una Build Configuration clásica

Si utilizas la configuración clásica **Build**, sustituye el paso que ejecuta `docker-compose -f docker-compose.yml up -d --build` por un runner **Command Line → Custom script**, ejecutado directamente en **My agent**, con directorio de trabajo `%teamcity.build.checkoutDir%`:

```bat
set "ATS_IMAGE_TAG=tc-%teamcity.build.id%"
set "ATS_PUBLIC_KEYCLOAK_URL=http://localhost:18085"
node automation/scripts/ci-stack.mjs
```

Guarda el cambio en **Build → Edit Configuration Settings → Build Steps**. Si no permite cambiar el tipo del runner, agrega el paso Command Line y deshabilita el paso Docker Compose anterior. Puedes colocar la integración después de los pasos de compilación y pruebas .NET. Las referencias `%...%` de este ejemplo se resuelven dentro del campo Custom script de TeamCity.

El Compose de desarrollo tiene nombres fijos, como `ats_seq`, y puertos fijos. Usarlo desde diferentes checkouts causa conflictos con los contenedores existentes, incluso si están detenidos. La integración usa `automation/compose.ci.yml`, un nombre de proyecto aleatorio, puertos dinámicos, datos temporales e IA simulada. El script ejecuta las comprobaciones HTTP y limpia únicamente su propio proyecto. No hace falta eliminar los contenedores ni los datos de desarrollo. Cambiar solamente el nombre del proyecto del Compose de desarrollo no resuelve sus nombres de contenedor fijos.

La web ejecuta el pipeline síncrono mediante `/api/v1/ingestion/evaluate`. Los casos de uso de Application coordinan el análisis del CV, la interpretación DISC y la generación del informe. Guardan datos entre etapas; una falla puede requerir un reintento.

## Prueba HTTP de integración

`scripts/smoke-api.mjs` ejecuta un flujo de integración con 23 solicitudes a la API, tokens reales de Keycloak, IA simulada explícitamente, reprocesamiento y descarga PDF. Crea candidatos y vacantes; ejecútelo sobre una base aislada. El total de solicitudes no representa 23 pruebas independientes; consulte [la revisión de pruebas](../docs/revision-pruebas.md).

```powershell
dotnet test Ats.slnx
node automation/scripts/smoke-api.mjs
```

Antes de ejecutar el script HTTP por separado, inicie una API en 15027 con PostgreSQL de pruebas y `Gemini__UseMockData=true`, y Keycloak con el realm del proyecto en 18085. El CV PDF ficticio se genera en memoria; `ATS_SMOKE_PDF_PATH` permite utilizar otro archivo. Puede configurar `ATS_SMOKE_API_URL`, `ATS_SMOKE_KEYCLOAK_URL`, `ATS_SMOKE_ADMIN_PASSWORD` y `ATS_SMOKE_RECRUITER_PASSWORD`.

Consulte [la revisión técnica](../docs/revision-tecnica.md) para resultados y limitaciones.
