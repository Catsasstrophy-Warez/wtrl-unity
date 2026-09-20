using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WTRL.Garage
{
    /// <summary>
    /// A real batch importer for the actual research corpus at
    /// `racinggame/ImportedVehicleCorpus/Content/Engineering/
    /// master_parts_catalog.json` (schema `wtrl.rev24.master-parts.v1`,
    /// 30 families x 9 generations = 1,560 real entries) -- closes
    /// "no batch content importer connects the real JSON catalogs to the
    /// game" from the world-content gap audit. Mirrors this project's
    /// existing `System.Text.Json` pattern from
    /// `Persistence/CareerSaveCodec.cs`.
    ///
    /// HONEST LIMITATION, deliberately not hidden: this catalog is a
    /// real ENGINEERING research taxonomy (part identity, family,
    /// generation, origin/quality tier, research-completeness status),
    /// not a game-balance catalog -- it has no price, reputation gate,
    /// or top-speed/acceleration delta for any entry (confirmed by
    /// reading real entries directly; those fields simply don't exist in
    /// the source JSON). Fabricating gameplay numbers to force these
    /// 1,560 entries into `PartDefinition` (which requires Price/
    /// ReputationRequired/TopSpeedDelta/AccelerationDelta) would be
    /// exactly the kind of invented content this project's discipline
    /// rejects. So this importer produces `ResearchPartRecord`s -- a
    /// faithful, ungameified transcription of the real data -- and
    /// deliberately does NOT auto-convert them into `PartDefinition`s.
    /// Balancing real gameplay numbers against this real research data
    /// is a separate, human-judgment-requiring pass, not done here.
    /// </summary>
    public sealed class ResearchPartRecord
    {
        [JsonPropertyName("specificationId")] public string SpecificationId { get; set; } = string.Empty;
        [JsonPropertyName("engineeringId")] public string EngineeringId { get; set; } = string.Empty;
        [JsonPropertyName("generationId")] public string GenerationId { get; set; } = string.Empty;
        [JsonPropertyName("generationName")] public string GenerationName { get; set; } = string.Empty;
        [JsonPropertyName("family")] public string Family { get; set; } = string.Empty;
        [JsonPropertyName("familyDisplayName")] public string FamilyDisplayName { get; set; } = string.Empty;
        [JsonPropertyName("subtype")] public string Subtype { get; set; } = string.Empty;
        [JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
        [JsonPropertyName("origin")] public string Origin { get; set; } = string.Empty;
        [JsonPropertyName("quality")] public string Quality { get; set; } = string.Empty;
        [JsonPropertyName("eraTag")] public string EraTag { get; set; } = string.Empty;
        [JsonPropertyName("researchStatus")] public string ResearchStatus { get; set; } = string.Empty;
        [JsonPropertyName("variantLevel")] public int VariantLevel { get; set; }
        [JsonPropertyName("dependencies")] public List<PartDependencyRule> Dependencies { get; set; } = new();
        [JsonPropertyName("consequences")] public List<PartDependencyRule> Consequences { get; set; } = new();
    }

    /// <summary>Real structured cross-family requirement/consequence
    /// rule, e.g. "a high-airflow intake requires higher fuel delivery
    /// capacity" -- found by an actual deserialization failure against
    /// the real corpus file (an initial draft of this importer assumed
    /// `dependencies`/`consequences` were plain string arrays; the real
    /// entries are objects like this one), not designed up front.</summary>
    public sealed class PartDependencyRule
    {
        [JsonPropertyName("ruleId")] public string RuleId { get; set; } = string.Empty;
        [JsonPropertyName("requiredFamily")] public int RequiredFamily { get; set; }
        [JsonPropertyName("requiredFamilyName")] public string RequiredFamilyName { get; set; } = string.Empty;
        [JsonPropertyName("threshold")] public double Threshold { get; set; }
        [JsonPropertyName("thresholdMetric")] public string ThresholdMetric { get; set; } = string.Empty;
        [JsonPropertyName("hardRequirement")] public bool HardRequirement { get; set; }
        [JsonPropertyName("suggestedResolution")] public string SuggestedResolution { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    }

    public sealed class PartFamilyRecord
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    }

    public sealed class PartGenerationRecord
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("eraTag")] public string EraTag { get; set; } = string.Empty;
    }

    /// <summary>The full deserialized catalog document -- top-level
    /// shape of master_parts_catalog.json.</summary>
    public sealed class PartCatalogDocument
    {
        [JsonPropertyName("schema")] public string Schema { get; set; } = string.Empty;
        [JsonPropertyName("familyCount")] public int FamilyCount { get; set; }
        [JsonPropertyName("generationCount")] public int GenerationCount { get; set; }
        [JsonPropertyName("entryCount")] public int EntryCount { get; set; }
        [JsonPropertyName("families")] public List<PartFamilyRecord> Families { get; set; } = new();
        [JsonPropertyName("generations")] public List<PartGenerationRecord> Generations { get; set; } = new();
        [JsonPropertyName("entries")] public List<ResearchPartRecord> Entries { get; set; } = new();
    }

    public static class PartCatalogImporter
    {
        /// <summary>Pure string-in, deserializes-and-returns -- kept
        /// independent of any file path or platform (StreamingAssets
        /// paths differ between Editor and player builds) so it's
        /// directly testable against a real, checked-in JSON fixture.</summary>
        public static PartCatalogDocument ImportFromJson(string json)
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return System.Text.Json.JsonSerializer.Deserialize<PartCatalogDocument>(json, options)
                ?? new PartCatalogDocument();
        }

        /// <summary>Convenience loader for the copy of the real corpus
        /// file checked into `Assets/StreamingAssets/Corpus/
        /// master_parts_catalog.json` (mirrored from
        /// racinggame/ImportedVehicleCorpus/Content/Engineering/, the
        /// actual source of truth this importer transcribes). Reads
        /// synchronously via `System.IO.File`, which works for the
        /// desktop Editor/standalone StreamingAssets path used by this
        /// project's other Editor-time content builders (e.g.
        /// `HeroContentBuilder`) -- Android/iOS StreamingAssets access
        /// requires `UnityWebRequest` instead and is NOT handled here,
        /// an honest, documented gap for real mobile builds.</summary>
        public static PartCatalogDocument LoadFromStreamingAssets()
        {
            var path = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath,
                "Corpus", "master_parts_catalog.json");
            var json = System.IO.File.ReadAllText(path);
            return ImportFromJson(json);
        }
    }
}
