# Revisión de utilidad de las pruebas — 2 de octubre de 2026

La suite tenía pruebas útiles y algunos casos de poco valor. Tras revisar sus entradas, aserciones y el código que ejercitan, quedaron **56 casos xUnit en 43 métodos**, algunos parametrizados. Las solicitudes del script HTTP se cuentan por separado: son **23 solicitudes a la API dentro de un flujo de integración**, además de los logins de Keycloak.

## Decisiones aplicadas

| Hallazgo | Cambio y motivo |
| --- | --- |
| `GenerateSampleCvTest` solo generaba un CV y comprobaba que el archivo existiera y tuviera tamaño | Eliminado. No ejercitaba código del producto; el CV de integración ya se genera en memoria |
| `CvGenerationTests` mezclaba un CV extenso, escritura en el repositorio y extracción de texto dentro del proyecto de arquitectura | Sustituido por `PdfTextExtractorTests` en infraestructura: un PDF pequeño de dos páginas debe devolver el texto de ambas, sin escribir archivos |
| Siete casos de lectura fuera del almacenamiento utilizaban rutas del sistema o archivos posiblemente inexistentes | Consolidados en tres entradas contra un archivo temporal que sí existe: ruta absoluta, escape con `/` y escape con `\`. Un archivo inexistente podía devolver `null` aunque faltara la protección |
| Prueba de informe denominada `ShouldGenerateTwoPageReport`, con el renderer simulado devolviendo tres bytes | Renombrada para describir lo que comprueba. Ahora verifica contenido CV/DISC, preguntas concretas, archivo y entidad persistida; se retiró el número exacto de llamadas internas a `SaveChanges` |
| Registro y envío DISC aceptaban cualquier entidad en sus mocks | Ahora comprueban los datos y el candidato de la entidad entregada a persistencia |
| Prueba de caché de reportes guardaba `{}` y solo verificaba éxito y ausencia de regeneración | Usa contenido realista y comprueba que devuelve el nombre y la URL del reporte existente |
| Validación del modelo EF dentro de arquitectura | Reubicada en infraestructura. Se conserva porque detecta errores de constructores y mapeos sin levantar PostgreSQL |
| La última consulta de vacantes del script HTTP solo comprobaba HTTP 200 | Ahora verifica que la vacante creada exista y conserve cero años de experiencia mínima. También comprueba que la evaluación conserve el dictamen anterior |
| El reporte de integración tenía un total de comprobaciones escrito como constante | Obtiene `apiRequests` del script ejecutado, sin presentar cada solicitud HTTP como una prueba independiente |

La disminución de 61 a 56 casos corresponde al generador eliminado y a cuatro variantes de rutas reemplazadas por casos más exigentes. La extracción PDF y el modelo EF se conservaron en el proyecto apropiado.

## Qué se conserva y por qué

| Grupo | Casos | Riesgo que cubre |
| --- | ---: | --- |
| Dominio | 15 | Nombres y correos inválidos, normalización, puntajes DISC, estados y dictámenes desconocidos, eventos al modificar entidades |
| Aplicación | 18 | Validación antes de persistir, límite de carga, datos ausentes, puntajes reales, pertenencia de resultados DISC, invalidación de caché y errores al generar PDF |
| Infraestructura | 19 | Detección y neutralización de prompt injection, lectura/escritura de archivos, escape del directorio, extracción PDF, modelo EF y simulación de Gemini explícita |
| Arquitectura | 4 | Dirección de dependencias entre capas y delegación de los controladores a los casos de uso |
| **Total xUnit** | **56** | |

Las variaciones de entrada de las teorías no se eliminaron por compartir un método: por ejemplo, `null`, vacío y espacios, o ataques en inglés y español, son entradas distintas que conviene proteger.

El flujo HTTP complementa las pruebas con mocks: ejercita serialización, JWT y roles reales, SQL/EF, el proxy Nginx, ingesta, reprocesamiento y descarga. El solapamiento de una regla entre dominio y HTTP comprueba su aplicación a través de capas diferentes.

## Coste del pipeline

Los pushes a `feature/*` ejecutan compilación y pruebas rápidas. El entorno Docker completo se ejecuta después de esa validación para pull requests, `main`, `develop` y ejecuciones manuales. La publicación continúa exigiendo integración exitosa desde `main`.

Las condiciones del pipeline utilizan el estado anterior, la rama y el motivo de ejecución, como documenta [Microsoft](https://learn.microsoft.com/en-us/azure/devops/pipelines/process/conditions?view=azure-devops).

## Verificación y límites

Pasaron los 56 casos en Release y el flujo de 23 solicitudes a la API contra PostgreSQL, Keycloak, backend y Nginx en Docker. La limpieza del entorno temporal terminó correctamente y la sintaxis YAML se validó.

Esta revisión evalúa propósito y aserciones; no se ejecutó análisis de mutaciones ni se midió cobertura. La integración utiliza IA simulada y no incluye interacción visual en navegador. La prueba de coordinación del reporte no garantiza su número físico de páginas; la descarga comprueba cabecera y tamaño de un PDF real.
