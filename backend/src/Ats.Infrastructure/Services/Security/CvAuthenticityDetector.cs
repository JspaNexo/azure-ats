namespace Ats.Infrastructure.Services.Security;

public static class CvAuthenticityDetector
{
    private static readonly string[] GenericBoilerplatePhrases = [
        "results-driven professional",
        "proven track record of success",
        "passionate about leveraging cutting-edge",
        "dynamic and detail-oriented",
        "adept at problem-solving and thinking outside the box",
        "profesional altamente motivado y orientado a resultados",
        "historial comprobado de entrega exitosa",
        "apasionado por aprovechar tecnologías de vanguardia",
        "capacidad demostrada para trabajar en entornos dinámicos",
        "habilidad para pensar fuera de la caja"
    ];

    public static List<string> Detect(string rawCvText)
    {
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(rawCvText))
        {
            return warnings;
        }

        int count = GenericBoilerplatePhrases.Count(phrase =>
            rawCvText.Contains(phrase, StringComparison.OrdinalIgnoreCase));

        if (count >= 3)
        {
            warnings.Add($"Alerta de autenticidad: Se detectaron {count} clichés o fórmulas genéricas comunes de plantillas automatizadas o generadores de texto.");
        }

        return warnings;
    }
}
