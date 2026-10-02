# Comprobaciones HTTP

La integración completa para Azure DevOps y ejecución local está documentada en [automatizaciones DevOps](../docs/automatizaciones-devops.md). Puede iniciarla con `node automation/scripts/ci-stack.mjs`; crea y limpia sus servicios temporales automáticamente.

La web ejecuta el pipeline síncrono mediante `/api/v1/ingestion/evaluate`. Los casos de uso de Application coordinan el análisis del CV, la interpretación DISC y la generación del informe. Guardan datos entre etapas; una falla puede requerir un reintento.

## Prueba HTTP de integración

`scripts/smoke-api.mjs` ejecuta un flujo de integración con 23 solicitudes a la API, tokens reales de Keycloak, IA simulada explícitamente, reprocesamiento y descarga PDF. Crea candidatos y vacantes; ejecútelo sobre una base aislada. El total de solicitudes no representa 23 pruebas independientes; consulte [la revisión de pruebas](../docs/revision-pruebas.md).

```powershell
dotnet test Ats.slnx
node automation/scripts/smoke-api.mjs
```

Antes de ejecutar el script HTTP por separado, inicie una API en 15027 con PostgreSQL de pruebas y `Gemini__UseMockData=true`, y Keycloak con el realm del proyecto en 18085. El CV PDF ficticio se genera en memoria; `ATS_SMOKE_PDF_PATH` permite utilizar otro archivo. Puede configurar `ATS_SMOKE_API_URL`, `ATS_SMOKE_KEYCLOAK_URL`, `ATS_SMOKE_ADMIN_PASSWORD` y `ATS_SMOKE_RECRUITER_PASSWORD`.

Consulte [la revisión técnica](../docs/revision-tecnica.md) para resultados y limitaciones.
