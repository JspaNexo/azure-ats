using Ats.Domain.Common;

namespace Ats.Domain.ValueObjects;

public sealed class AssessmentScores : ValueObject
{
    public string AssessmentType { get; private set; } = string.Empty;
    public IReadOnlyDictionary<string, double> Dimensions { get; private set; } = new Dictionary<string, double>();
    public string PrimaryStyle { get; private set; } = string.Empty;

    // Propiedades de retrocompatibilidad directa con DISC
    public int Dominance => GetDimensionIntValue("Dominance") ?? GetDimensionIntValue("D") ?? 0;
    public int Influence => GetDimensionIntValue("Influence") ?? GetDimensionIntValue("I") ?? 0;
    public int Steadiness => GetDimensionIntValue("Steadiness") ?? GetDimensionIntValue("S") ?? 0;
    public int Conscientiousness => GetDimensionIntValue("Conscientiousness") ?? GetDimensionIntValue("C") ?? 0;

    private AssessmentScores() { }

    private AssessmentScores(string assessmentType, IReadOnlyDictionary<string, double> dimensions, string primaryStyle)
    {
        AssessmentType = assessmentType;
        Dimensions = dimensions;
        PrimaryStyle = primaryStyle;
    }

    public static Result<AssessmentScores> Create(string assessmentType, IDictionary<string, double> dimensions, string? primaryStyle = null)
    {
        if (string.IsNullOrWhiteSpace(assessmentType))
        {
            return Result.Failure<AssessmentScores>(
                Error.Validation("AssessmentScores.TypeRequired", "El tipo de evaluacion es obligatorio."));
        }

        if (dimensions == null || dimensions.Count == 0)
        {
            return Result.Failure<AssessmentScores>(
                Error.Validation("AssessmentScores.DimensionsRequired", "Debe proporcionar al menos una dimension puntuada."));
        }

        foreach (var (key, value) in dimensions)
        {
            if (value < 0 || value > 100)
            {
                return Result.Failure<AssessmentScores>(
                    Error.Validation("AssessmentScores.OutOfRange", $"La dimension '{key}' ({value}) debe estar en el rango de 0 a 100."));
            }
        }

        string calculatedStyle = !string.IsNullOrWhiteSpace(primaryStyle)
            ? primaryStyle
            : DeterminePrimaryStyle(dimensions);

        var readOnlyDims = new Dictionary<string, double>(dimensions, StringComparer.OrdinalIgnoreCase);
        return Result.Success(new AssessmentScores(assessmentType.Trim(), readOnlyDims, calculatedStyle));
    }

    public static Result<AssessmentScores> CreateDisc(int d, int i, int s, int c, string? primaryStyle = null)
    {
        if (d < 0 || d > 100 || i < 0 || i > 100 || s < 0 || s > 100 || c < 0 || c > 100)
        {
            return Result.Failure<AssessmentScores>(
                Error.Validation("DiscScores.OutOfRange", "Los valores DISC deben estar en el rango de 0 a 100."));
        }

        var dims = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dominance"] = d,
            ["Influence"] = i,
            ["Steadiness"] = s,
            ["Conscientiousness"] = c
        };

        string style = primaryStyle ?? DetermineDiscStyle(d, i, s, c);
        return Result.Success(new AssessmentScores("DISC", dims, style));
    }

    private int? GetDimensionIntValue(string key)
    {
        if (Dimensions.TryGetValue(key, out var val))
        {
            return (int)Math.Round(val);
        }
        return null;
    }

    private static string DeterminePrimaryStyle(IDictionary<string, double> dimensions)
    {
        var top = dimensions.OrderByDescending(x => x.Value).Take(2).ToList();
        if (top.Count == 0) return "General";
        if (top.Count == 1) return top[0].Key;

        if (top[0].Value - top[1].Value <= 5)
        {
            return $"{top[0].Key}/{top[1].Key}";
        }

        return top[0].Key;
    }

    private static string DetermineDiscStyle(int d, int i, int s, int c)
    {
        var scores = new (string Style, int Score)[]
        {
            ("D", d),
            ("I", i),
            ("S", s),
            ("C", c)
        }.OrderByDescending(x => x.Score).ToList();

        if (scores[0].Score - scores[1].Score <= 5)
        {
            return $"{scores[0].Style}/{scores[1].Style}";
        }

        return scores[0].Style;
    }

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return AssessmentType.ToLowerInvariant();
        yield return PrimaryStyle.ToLowerInvariant();
        foreach (var kvp in Dimensions.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            yield return kvp.Key.ToLowerInvariant();
            yield return kvp.Value;
        }
    }
}
