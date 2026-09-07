using System.Text.RegularExpressions;
using Ats.Domain.Common;

namespace Ats.Domain.ValueObjects;

public sealed class CandidateEmail : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; private set; } = string.Empty;

    private CandidateEmail() { }

    private CandidateEmail(string value) => Value = value;

    public static Result<CandidateEmail> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<CandidateEmail>(
                Error.Validation("Email.Empty", "El correo electrónico no puede estar vacío."));
        }

        string trimmedEmail = email.Trim();
        if (!EmailRegex.IsMatch(trimmedEmail))
        {
            return Result.Failure<CandidateEmail>(
                Error.Validation("Email.InvalidFormat", "El formato del correo electrónico no es válido."));
        }

        return Result.Success(new CandidateEmail(trimmedEmail));
    }

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}

public sealed class DiscScores : ValueObject
{
    public int Dominance { get; private set; }
    public int Influence { get; private set; }
    public int Steadiness { get; private set; }
    public int Conscientiousness { get; private set; }
    public string PrimaryStyle { get; private set; } = string.Empty;

    private DiscScores() { }

    private DiscScores(int dominance, int influence, int steadiness, int conscientiousness, string primaryStyle)
    {
        Dominance = dominance;
        Influence = influence;
        Steadiness = steadiness;
        Conscientiousness = conscientiousness;
        PrimaryStyle = primaryStyle;
    }

    public static Result<DiscScores> Create(int d, int i, int s, int c, string? primaryStyle = null)
    {
        if (d < 0 || d > 100 || i < 0 || i > 100 || s < 0 || s > 100 || c < 0 || c > 100)
        {
            return Result.Failure<DiscScores>(
                Error.Validation("DiscScores.OutOfRange", "Los valores DISC deben estar en el rango de 0 a 100."));
        }

        string calculatedStyle = primaryStyle ?? DeterminePrimaryStyle(d, i, s, c);
        return Result.Success(new DiscScores(d, i, s, c, calculatedStyle));
    }

    private static string DeterminePrimaryStyle(int d, int i, int s, int c)
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
        yield return Dominance;
        yield return Influence;
        yield return Steadiness;
        yield return Conscientiousness;
        yield return PrimaryStyle;
    }
}

public sealed class SkillEvidence : ValueObject
{
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public int ExperienceYears { get; private set; }
    public string Evidence { get; private set; } = string.Empty;
    public double Confidence { get; private set; }

    private SkillEvidence() { }

    public SkillEvidence(
        string name,
        string normalizedName,
        string category,
        int experienceYears,
        string evidence,
        double confidence)
    {
        Name = name;
        NormalizedName = normalizedName;
        Category = category;
        ExperienceYears = experienceYears;
        Evidence = evidence;
        Confidence = confidence;
    }

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return NormalizedName.ToLowerInvariant();
        yield return Category.ToLowerInvariant();
        yield return ExperienceYears;
    }
}

public sealed class PromptVersion : ValueObject
{
    public string Value { get; private set; } = string.Empty;

    private PromptVersion() { }

    private PromptVersion(string value) => Value = value;

    public static Result<PromptVersion> Create(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return Result.Failure<PromptVersion>(
                Error.Validation("PromptVersion.Empty", "La versión del prompt no puede estar vacía."));
        }

        return Result.Success(new PromptVersion(version.Trim()));
    }

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
