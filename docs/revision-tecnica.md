# Revisión técnica — 2 de octubre de 2026

Se corrigieron los bloqueos de compilación y del flujo principal. Las pruebas comprueban que Domain no dependa de otras capas, que Application no dependa de Infrastructure o Api y que los controladores deleguen el acceso a datos y la generación de documentos a los casos de uso.

## Hallazgos corregidos

| Problema | Corrección |
| --- | --- |
| Faltaba `LocalStorageService`; Git excluía su directorio de código | Implementación con nombres únicos, validación de rutas y rechazo de enlaces; excepción específica en `.gitignore` |
| Scripts de datos y claves foráneas incompatibles con `01-init.sql` | Columnas SQL y mapeo EF alineados, incluidas las claves compartidas de los value objects |
| Versiones incompatibles de EF Core | Referencia explícita a EF Core Relational 10.0.11 |
| Validadores registrados pero no ejecutados | Validación en registro, CV, DISC, vacantes e ingesta; rechazo antes de escribir |
| Límites de archivo diferentes entre interfaz e ingesta | Límite de 15 MB y comprobación de cabecera `%PDF-` |
| Ingesta con vacantes inexistentes o inactivas | Rechazo antes de registrar al postulante |
| Cero años mínimos se guardaba como tres | Sentinel explícito de EF para preservar el cero |
| Duplicados de correo con distintas mayúsculas | Normalización en el value object |
| Estados y dictámenes desconocidos aceptados silenciosamente | Validación de dominio sin mutar ante entradas inválidas |
| CV o DISC nuevos reutilizaban resultados anteriores | Caché vinculada a sus fuentes y nueva versión del reporte al cambiarlas |
| DISC ignoraba el ID solicitado | Consulta por ID y comprobación de pertenencia al candidato |
| Cuatro consultas adicionales por cada candidato y mapeo duplicado | Puerto de lectura, consultas por lotes y mapeo compartido |
| Generación y descarga PDF dentro del controlador | Caso de uso de descarga en Application |
| Métricas y puntajes inventados presentados como resultados reales | Ajuste y experiencia ausentes como `null`; gráfico con puntajes DISC persistidos |
| Ausencia de clave Gemini producía simulaciones implícitas | Error de configuración; simulación optativa y advertencia en el análisis |
| Health check sin consultar PostgreSQL | Consulta del esquema a través de un puerto de Application |
| Cliente con endpoints inexistentes y renovación de token fallida ignorada | Métodos sin consumidores eliminados; error explícito de sesión expirada |
| Imágenes .NET preview e instalación npm no reproducible | .NET 10 estable y `npm ci` |
| Orquestador externo sin una función indispensable | Retirados el servicio de Compose, su configuración, declaración de volumen y plantillas; el flujo permanece en los casos de uso de Application |

## Verificación

Resultado actual tras la [revisión de utilidad de las pruebas](revision-pruebas.md): **56 casos xUnit** y un flujo de integración de **23 solicitudes a la API**, todos pasando. Los resultados de 61 casos descritos abajo corresponden a la verificación inicial, antes de retirar y consolidar casos de poco valor.

- Build .NET sin errores ni advertencias y 61 pruebas de toda la solución, incluyendo `backend/tests/Ats.Tests`, antes excluidas.
- Compilación TypeScript y bundle Vite con `npm.cmd run build`.
- PostgreSQL 16 temporal: todos los scripts de inicialización y consultas EF contra ese esquema.
- Keycloak 26.1 temporal: tokens reales de administrador y reclutador, respuestas 401 y 403 verificadas.
- `node automation/scripts/smoke-api.mjs`: 23 comprobaciones HTTP de registro, duplicados, cero años mínimos, asignación, dictamen, entradas inválidas sin crear candidatos, ingesta, reprocesamiento, caché y PDF válido.
- Construcción Docker de backend y frontend, comprobación de sintaxis Nginx y validación de Compose. Las 23 comprobaciones HTTP también pasaron contra el contenedor Linux final del backend.
- Tras retirar el orquestador externo, Compose sigue validando correctamente y las 61 pruebas .NET vuelven a pasar.
- Automatización DevOps: compilación Release sin advertencias, 61 pruebas y 23 comprobaciones HTTP mediante Nginx en el entorno Docker temporal de `automation/scripts/ci-stack.mjs`. El pipeline de Azure DevOps está preparado en el repositorio; su activación y publicación se explican en [automatizaciones DevOps](automatizaciones-devops.md).

La integración utiliza IA simulada de forma explícita. No se llamó a Gemini real ni se hizo una prueba visual en navegador.

## Compatibilidad y alcance

Los scripts SQL inicializan una base vacía; Docker no los vuelve a ejecutar sobre un volumen existente. Una base antigua con columnas PascalCase necesita una migración explícita. `EnsureCreated` no actualiza esquemas y solo está habilitado por configuración en Development.

El contrato de candidato ahora permite `matchScore` y `experienceYears` nulos e incluye `discScores`. El frontend contempla estos valores; los clientes externos deben actualizar su manejo de datos ausentes.

Configure `Gemini__ApiKey` para IA real. `Gemini__UseMockData=true` habilita simulación de desarrollo y está desactivado por defecto. La prueba HTTP utiliza servicios aislados y escribe datos de prueba; sus destinos y credenciales son configurables mediante las variables documentadas en `automation/README.md`.

La ingesta persiste entre etapas. Una falla del proveedor puede dejar al candidato y documentos guardados para reintentar; no es una transacción atómica ni una cola con recuperación automática. El catálogo de reclutadores mantiene las dos cuentas del prototipo y el listado aún no tiene paginación.

La validación de audience JWT está desactivada, CORS permite cualquier origen y Compose incluye credenciales de demostración. Son decisiones existentes que requieren cambios al ampliar el prototipo a producción.

La autenticación de webhooks implementada es un secreto compartido o un token autorizado. No implementa HMAC del cuerpo; se corrigió la documentación que afirmaba lo contrario. Estos endpoints permiten ejecutar etapas del procesamiento desde integraciones externas autenticadas.
