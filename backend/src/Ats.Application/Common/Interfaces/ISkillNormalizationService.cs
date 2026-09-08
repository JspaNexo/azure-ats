namespace Ats.Application.Common.Interfaces;

public interface ISkillNormalizationService
{
    (string CanonicalName, string Category) Normalize(string rawSkillName);
    bool IsKnownSkill(string rawSkillName);
}
