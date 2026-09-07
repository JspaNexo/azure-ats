# Modulo de Automatizacion (Flujos n8n y Webhooks)

Este directorio contiene las definiciones declarativas de flujos de trabajo en formato JSON para el motor de orquestacion **n8n** y la arquitectura de comunicacion por webhooks seguros con el backend de TalentIQ ATS.

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

El sistema soporta dos modalidades de ejecucion complementarias:

1. **Ingesta Sincrona en Tiempo Real (Recomendada para la Web UI):**
   - El frontend consume directamente el endpoint `POST /api/v1/ingestion/evaluate`.
   - Se procesa la extraccion de PDF, sanitizacion anti-prompt injection, analisis con Gemini AI, calculo DISC y generacion de preguntas STAR en una sola transaccion interactiva con reporte inmediato.

2. **Ingesta Asincrona por Lotes (Orquestada por n8n):**
   - Adecuada para cargas masivas nocturnas o integracion con sistemas externos de terceros.
   - n8n detecta o recibe el documento y coordina las etapas invocando los webhooks correspondientes en el backend.

---

## 3. Seguridad Criptografica de Webhooks

Para garantizar la integridad y autenticidad de los datos provenientes de n8n, el backend implementa una validacion estricta en [`WebhooksController.cs`](file:///c:/Users/jspaniagua/Documents/proyectos/ats/backend/src/Ats.Api/Controllers/WebhooksController.cs):

- **Encabezado Requerido:** `X-ATS-Signature`.
- **Algoritmo:** HMAC SHA-256 calculado sobre el cuerpo bruto de la solicitud HTTP (raw body) utilizando el secreto compartido configurado en `Webhooks__Secret`.
- **Prevencion de Ataques de Canal Lateral (Timing Attacks):** La comparacion entre el hash computado y el recibido en el encabezado se realiza mediante `CryptographicOperations.FixedTimeEquals`.
- **Endpoints Protegidos:**
  - `POST /api/v1/webhooks/cv-processed`
  - `POST /api/v1/webhooks/disc-processed`

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

