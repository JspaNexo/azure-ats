using System.Reflection;
using System.Text.Json;
using Ats.Application.Common.Interfaces;

namespace Ats.Infrastructure.Services.Skills;

public class SkillCatalogEntry
{
    public string Canonical { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = [];
    public string Category { get; set; } = "General";
}

public class SkillNormalizationService : ISkillNormalizationService
{
    private readonly Dictionary<string, (string Canonical, string Category)> _lookup = new(StringComparer.OrdinalIgnoreCase);

    public SkillNormalizationService()
    {
        LoadCatalog();
    }

    private void LoadCatalog()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "Ats.Infrastructure.Data.skill-catalog.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                var entries = JsonSerializer.Deserialize<List<SkillCatalogEntry>>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        if (string.IsNullOrWhiteSpace(entry.Canonical)) continue;

                        _lookup[entry.Canonical.Trim()] = (entry.Canonical.Trim(), entry.Category);
                        foreach (var alias in entry.Aliases)
                        {
                            if (!string.IsNullOrWhiteSpace(alias))
                            {
                                _lookup[alias.Trim()] = (entry.Canonical.Trim(), entry.Category);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Resilient fallback to empty catalog
        }
    }

    public (string CanonicalName, string Category) Normalize(string rawSkillName)
    {
        if (string.IsNullOrWhiteSpace(rawSkillName))
        {
            return (string.Empty, "General");
        }

        string trimmed = rawSkillName.Trim();
        if (_lookup.TryGetValue(trimmed, out var match))
        {
            return match;
        }

        // Return cleaned original name and category General
        return (trimmed, "General");
    }

    public bool IsKnownSkill(string rawSkillName)
    {
        if (string.IsNullOrWhiteSpace(rawSkillName)) return false;
        return _lookup.ContainsKey(rawSkillName.Trim());
    }
}
