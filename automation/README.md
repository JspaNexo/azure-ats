# Modulo de Automatizacion (Flujos n8n y Webhooks)

Este directorio contiene las definiciones declarativas de flujos de trabajo en formato JSON para el motor de orquestacion **n8n** y la arquitectura de comunicacion por webhooks seguros con el backend de TalentIQ ATS.

> [!NOTE]
> **Aviso de Prototipo Funcional (Automatizacion)**
> Los flujos de trabajo en n8n incluidos en este directorio son ejemplos ilustrativos de orquestacion asincrona y procesamiento por lotes para una prueba de concepto. No limitan ni imponen un flujo operativo fijo; pueden modificarse, ampliarse o reemplazarse por otros orquestadores empresariales (tales como Apache Airflow, Temporal, Azure Logic Apps o AWS Step Functions) segun los requerimientos y herramientas de la organizacion.

---

## 1. Estructura de Flujos

```text
automation/
└── n8n/
    └── workflows/
        ├── cv-processing.json      # Orquestacion asincrona de extraccion y analisis curricular
        ├── disc-processing.json    # Orquestacion asincrona de perfil conductual DISC
        └── report-generation.json  # Orquestacion de consolidacion y generacion del reporte ejecutivo
```

---

## 2. Modos de Procesamiento Disponibles

El sistema soporta tres modalidades de ejecucion complementarias:

1. **Ingesta Sincrona en Tiempo Real (Recomendada para la Web UI):**
   - El cliente consume directamente el endpoint `POST /api/v1/ingestion/evaluate` (por defecto `Async=false`).
   - Se procesa la extraccion de PDF, sanitizacion anti-prompt injection, analisis con Gemini AI, calculo DISC y generacion de preguntas STAR en una sola transaccion interactiva con respuesta `200 OK` y reporte inmediato.

2. **Ingesta Asincrona Desacoplada con Cola Interna (Background Job Queue):**
   - Adecuada para solicitudes web con alto volumen o conexiones moviles inestables.
   - Al enviar el parametro `Async=true` en `POST /api/v1/ingestion/evaluate`, el backend registra el candidato y documento de forma inmediata, encola el analisis pesado en la cola interna `IBackgroundJobQueue` (basada en canales de memoria `System.Threading.Channels`) y responde de inmediato con `202 Accepted`.
   - El servicio alojado `QueuedHostedService` procesa el analisis en segundo plano sin saturar los hilos HTTP.

3. **Ingesta Asincrona por Lotes (Orquestada por n8n):**
   - Adecuada para integracion con plataformas ATS legadas, correos electronicos entrantes o cargas masivas nocturnas.
   - n8n detecta o recibe el documento y coordina las etapas invocando los webhooks correspondientes en el backend.

---

## 3. Seguridad de Webhooks

Para garantizar que unicamente orquestadores autorizados interactuen con los endpoints de procesamiento, [`WebhooksController.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Api/Controllers/WebhooksController.cs) implementa validacion de credenciales:

- **Encabezado Requerido:** `X-Webhook-Secret: ats_webhook_secret_2026` (o alternativamente `X-Api-Key: ats_internal_dev_key_2026`).
- **Prevencion de Ataques de Canal Lateral (Timing Attacks):** La comparacion entre el valor recibido y el secreto configurado se realiza mediante `CryptographicOperations.FixedTimeEquals`.
- **Endpoints Protegidos:**
  - `POST /api/v1/webhooks/process-cv` (parametros query: `candidateId`, `documentId`, `eventId`, `correlationId`)
  - `POST /api/v1/webhooks/process-disc` (parametros query: `candidateId`, `discResultId`, `eventId`, `correlationId`)
  - `POST /api/v1/webhooks/generate-report` (parametros query: `candidateId`, `eventId`, `correlationId`)

---

## 4. Procedimiento de Importacion y Activacion en n8n

1. Confirmar que el contenedor de automatizacion este activo y saludable:
   ```bash
   docker compose up -d n8n
   ```
2. Acceder a la interfaz web de n8n en [http://localhost:5678](http://localhost:5678).
3. En el panel lateral de Workflows, seleccionar la opcion **Import from File...** y elegir el archivo `.json` correspondiente dentro de `automation/n8n/workflows/`.
4. Configurar las credenciales o variables de entorno del webhook (URL del backend: `http://backend:8080` dentro de la red Docker, clave compartida).
5. Activar el flujo conmutando el estado a **Active**.

