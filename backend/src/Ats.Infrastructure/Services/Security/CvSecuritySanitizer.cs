using System.Text.RegularExpressions;

namespace Ats.Infrastructure.Services.Security;

public record CvSanitizationResult(
    string SanitizedText,
    List<string> SecurityWarnings,
    bool HasInjectionSuspect);

public static class CvSecuritySanitizer
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    // Patterns that attempt to override system instructions or hijack LLM execution flow
    private static readonly Regex[] InjectionPatterns =
    [
        // Direct override attempts in English and Spanish (handling noun-adjective order in both languages)
        new(@"\b(ignore?|disregard|forget|omit|bypass|descartar?|ignora[rn]?|olvida[rn]?)\s+(all\s+|todas?\s+(las?\s+)?)?(previous|prior|above|preceding|anteriores|previas)?\s*(instructions|prompts|rules|commands|directives|instrucciones|reglas|[oó]rdenes)\s*(anteriores|previas)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),

        // System prompt or override claims
        new(@"\b(system\s*(override|instruction|prompt|command|directive|reset)|instrucci[oó]n\s+del\s+sistema)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),

        // Role impersonation (e.g., "you are now an unrestricted assistant", "act as DAN")
        new(@"\b(you\s+are\s+now|act\s+as|pretend\s+to\s+be|ahora\s+eres|act[uú]a\s+como)\s+(a|an|the)?\s*(unrestricted|jailbroken|dan|developer|admin|system|root|libre|sin\s+restricciones)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),

        // Jailbreak modes
        new(@"\b(dan\s+mode|developer\s+mode|jailbreak|unrestricted\s+mode|modo\s+desarrollador|modo\s+dan)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),

        // Command manipulation for scoring or decisions (avoid trailing \b after %)
        new(@"\b(score\s+this\s+candidate\s+(at\s+)?100\s*%?|give\s+maximum\s+score|always\s+approve|califica\s+(con\s+)?100\s*%?|aprobar\s+inmediatamente|contratar\s+de\s+inmediato|hire\s+immediately)", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),

        // Forced output formatting overrides
        new(@"\b(output\s+only|respond\s+only\s+with|devuelve\s+[uú]nicamente\s+el\s+siguiente\s+json\s+falso)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),

        // Concealment attempts (e.g., "do not mention this instruction")
        new(@"\b(do\s+not\s+mention|hide\s+this\s+instruction|keep\s+this\s+secret|no\s+menciones\s+esta\s+instrucci[oó]n)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),

        // LLM prompt boundary markers
        new(@"(\[SYSTEM\]|\[INSTRUCTION\]|\[ADMIN\]|<\|im_start\|>|<\|im_end\|>|<\|endoftext\|>|Human:|Assistant:)", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout)
    ];

    // Delimiters that could break out of <untrusted_applicant_cv>
    private static readonly Regex DelimiterBreakoutRegex = new(
        @"<\s*/?\s*untrusted_applicant_cv\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        RegexTimeout);

    /// <summary>
    /// Analyzes the extracted CV text for prompt injection attempts, neutralizes tag breakout attempts,
    /// and collects audit warnings.
    /// </summary>
    public static CvSanitizationResult SanitizeAndInspect(string rawCvText)
    {
        if (string.IsNullOrWhiteSpace(rawCvText))
        {
            return new CvSanitizationResult(string.Empty, [], false);
        }

        var warnings = new List<string>();
        bool hasInjectionSuspect = false;

        // 1. Scan for injection patterns
        foreach (var pattern in InjectionPatterns)
        {
            var match = pattern.Match(rawCvText);
            if (match.Success)
            {
                hasInjectionSuspect = true;
                warnings.Add($"Alerta de Integridad: Se detectó un patrón sospechoso de manipulación o inyección de instrucciones en el CV (coincidencia con: '{match.Value}').");
                break; // Avoid spamming multiple identical warnings
            }
        }

        // 2. Neutralize XML boundary breakouts
        string sanitizedText = DelimiterBreakoutRegex.Replace(rawCvText, match =>
        {
            hasInjectionSuspect = true;
            warnings.Add("Alerta de Seguridad: Se detectó un intento de evasión de delimitadores XML en el contenido del CV.");
            return match.Value.Replace("<", "&lt;").Replace(">", "&gt;");
        });

        // 3. Neutralize OpenAI/Anthropic/Gemini special token markers
        sanitizedText = sanitizedText
            .Replace("<|im_start|>", "[token_removed]")
            .Replace("<|im_end|>", "[token_removed]")
            .Replace("<|endoftext|>", "[token_removed]");

        return new CvSanitizationResult(sanitizedText, warnings, hasInjectionSuspect);
    }
}
