# Backend TalentIQ ATS

API ASP.NET Core 10 con Clean Architecture: Domain contiene entidades e invariantes; Application define casos de uso y puertos; Infrastructure implementa PostgreSQL, almacenamiento, PDF e IA; Api adapta HTTP y compone los servicios. Las pruebas verifican la dirección de dependencias y que los controladores deleguen los casos de uso.

## Ejecución local

Desde la raíz:

```powershell
docker compose up -d postgres keycloak
$env:Gemini__ApiKey = 'su-clave'
dotnet run --project backend/src/Ats.Api/Ats.Api.csproj
```

La conexión local usa PostgreSQL en 5433 y Keycloak en 8085. Para simular IA durante desarrollo, configure `Gemini__UseMockData=true`. Sin clave y sin simulación explícita, el análisis devuelve `Gemini.NotConfigured`.

Los scripts `database/init/00` a `06` inicializan una base vacía. Docker no los vuelve a ejecutar sobre un volumen existente. EF no actualiza automáticamente el esquema desplegado. En Development puede crearse una base vacía con `Database__EnsureCreated=true`, sin datos de demostración.

## Contratos HTTP

Los endpoints requieren `ats_admin` o `ats_recruiter`, salvo las alternativas de integración indicadas.

| Método | Ruta | Comportamiento |
| --- | --- | --- |
| GET / POST | `/api/v1/candidates` | Listar o registrar; filtro opcional `recruiterId` |
| GET | `/api/v1/candidates/{id}` | Consultar expediente |
| PATCH | `/api/v1/candidates/{id}/decision` | Guardar dictamen |
| PATCH | `/api/v1/candidates/{id}/assign` | Asignar reclutador; solo administrador |
| GET | `/api/v1/users/recruiters` | Catálogo del prototipo; solo administrador |
| GET / POST | `/api/v1/positions` | Listar o crear; creación solo administrador, responde 201 |
| PATCH | `/api/v1/positions/{id}/status` | Cambiar estado; solo administrador |
| POST | `/api/v1/documents/cv` | Formulario `candidateId` y `file`, PDF hasta 15 MB |
| POST | `/api/v1/disc/results` | Registrar puntajes DISC |
| POST | `/api/v1/ingestion/evaluate` | Formulario de candidato, PDF y DISC; token autorizado o `X-Api-Key` |
| GET | `/api/v1/reports/candidate/{candidateId}` | Consultar reporte |
| POST | `/api/v1/reports/generate?candidateId=...` | Generar o recuperar reporte para sus fuentes actuales |
| GET | `/api/v1/reports/download/{candidateId}` | Descargar PDF |
| POST | `/api/v1/webhooks/process-cv` | Query: `candidateId`, `documentId`, `eventId`, `correlationId` |
| POST | `/api/v1/webhooks/process-disc` | Query: `candidateId`, `discResultId`, `eventId`, `correlationId` |
| POST | `/api/v1/webhooks/generate-report` | Query: `candidateId`, `eventId`, `correlationId` |
| GET | `/health` | Comprobar conexión y esquema de candidatos; sin autenticación |

Los webhooks aceptan un token autorizado o `X-Webhook-Secret`, configurado en `Webhooks__Secret`. No implementan firma HMAC. `eventId` debe ser único por proceso.

El candidato devuelve `matchScore=null` cuando no existe un cálculo de ajuste implementado y `experienceYears=null` hasta analizar su CV. `discScores` contiene los puntajes guardados. Las entradas inválidas devuelven ProblemDetails.

## Verificación

```powershell
dotnet build Ats.slnx
dotnet test Ats.slnx
```

La solución incluye dominio, casos de uso, arquitectura, almacenamiento y configuración Gemini. Consulte [la revisión técnica](../docs/revision-tecnica.md) para resultados y limitaciones.
