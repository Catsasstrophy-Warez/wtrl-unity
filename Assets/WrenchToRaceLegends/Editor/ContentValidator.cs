using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WTRL.Racing;

namespace WTRL.EditorTools
{
    /// <summary>
    /// Real content validators, closing "no content validators exist"
    /// from the world-content gap audit -- PIVOT-PLAN.md's own original
    /// vision for `WTRL.Editor` named "content builders, validators"
    /// together, but only builders existed until now.
    ///
    /// HONEST SCOPE: these check the specific real invariants this
    /// project's content has actually needed guarding so far --
    /// duplicated track-id literals staying in sync between
    /// `WTRL.Racing`/`WTRL.World` (the documented pattern for avoiding a
    /// cross-assembly dependency), and `CanonicalBuildRecipes` having no
    /// duplicate ids -- not a general-purpose schema validator. Most of
    /// these properties are already guarded by `SampleContentTests`/
    /// `BuildRecipeTests`; this is the same checks exposed as a real
    /// Editor menu command a content author can run without opening the
    /// test runner, per PIVOT-PLAN's original "Rebuild + Validate"
    /// editor-menu pattern reference.
    /// </summary>
    public static class ContentValidator
    {
        [MenuItem("Assets/WTRL/Validate Content")]
        public static void ValidateAll()
        {
            var issues = new List<string>();

            ValidateCanonicalBuildRecipes(issues);
            ValidateRacingLineTrackIds(issues);
            ValidateResearchPartCatalog(issues);

            if (issues.Count == 0)
            {
                Debug.Log("ContentValidator: all checks passed, no issues found.");
            }
            else
            {
                foreach (var issue in issues) Debug.LogWarning($"ContentValidator: {issue}");
                Debug.LogWarning($"ContentValidator: {issues.Count} issue(s) found.");
            }
        }

        private static void ValidateCanonicalBuildRecipes(List<string> issues)
        {
            var seenIds = new HashSet<string>();
            var countsByVehicle = new Dictionary<string, int>();

            foreach (var recipe in Garage.CanonicalBuildRecipes.All)
            {
                if (!seenIds.Add(recipe.Id))
                {
                    issues.Add($"CanonicalBuildRecipes: duplicate recipe id '{recipe.Id}'");
                }

                if (recipe.TargetWeightToPowerMinKgPerHp <= 0 || recipe.TargetWeightToPowerMaxKgPerHp <= 0)
                {
                    issues.Add($"CanonicalBuildRecipes: recipe '{recipe.Id}' has a non-positive weight-to-power bound");
                }

                if (recipe.TargetWeightToPowerMinKgPerHp > recipe.TargetWeightToPowerMaxKgPerHp)
                {
                    issues.Add($"CanonicalBuildRecipes: recipe '{recipe.Id}' has min > max weight-to-power bound");
                }

                countsByVehicle.TryGetValue(recipe.VehicleId, out var count);
                countsByVehicle[recipe.VehicleId] = count + 1;
            }

            foreach (var kvp in countsByVehicle)
            {
                if (kvp.Value != 5)
                {
                    issues.Add($"CanonicalBuildRecipes: vehicle '{kvp.Key}' has {kvp.Value} recipes, expected exactly 5");
                }
            }
        }

        /// <summary>Re-checks, at Editor-menu time, the exact invariant
        /// `SampleContentTests.AllWorldTracksHaveAMatchingRacingLineWithSameIdAndAccurateLength`
        /// already guards in the test suite: every `Racing.SampleContent`
        /// track-id constant must still resolve to a real
        /// `TrackLineDefinition` with a non-empty node list. Doesn't
        /// duplicate the length-matching check against `WTRL.World`
        /// (that would require an assembly reference this Editor tool
        /// deliberately doesn't have -- `WTRL.Racing` stays
        /// `WTRL.World`-independent by design).</summary>
        private static void ValidateRacingLineTrackIds(List<string> issues)
        {
            var lines = new (string Id, TrackLineDefinition Line)[]
            {
                (SampleContent.FoundryRowCircuitTrackId, SampleContent.FoundryRowCircuitLine()),
                (SampleContent.CutbackTriOvalTrackId, SampleContent.CutbackTriOvalLine()),
                (SampleContent.LongbowSpeedwayTrackId, SampleContent.LongbowSpeedwayLine()),
                (SampleContent.HighbankSuperspeedwayTrackId, SampleContent.HighbankSuperspeedwayLine()),
                (SampleContent.WhisperwoodForestCircuitTrackId, SampleContent.WhisperwoodForestCircuitLine()),
                (SampleContent.CliffsideCoastalCircuitTrackId, SampleContent.CliffsideCoastalCircuitLine()),
                (SampleContent.IroncladTechnicalCircuitTrackId, SampleContent.IroncladTechnicalCircuitLine()),
            };

            foreach (var (id, line) in lines)
            {
                if (line.Nodes.Count == 0)
                {
                    issues.Add($"Racing.SampleContent: track '{id}' has a racing line with zero nodes");
                }
                if (line.TrackId != id)
                {
                    issues.Add($"Racing.SampleContent: track '{id}' racing line's own TrackId is '{line.TrackId}' (mismatch)");
                }
            }
        }

        /// <summary>Guards the real invariant `ResearchPartConverter`
        /// needs to hold across the entire 1,560-entry corpus: every
        /// converted part has a unique id and a positive price, and
        /// (the specific thing this converter promises never to
        /// fabricate) exactly zero performance effect. See
        /// `ResearchPartConverter.cs`'s own doc comment for why
        /// TopSpeedDelta/AccelerationDelta must stay zero.</summary>
        private static void ValidateResearchPartCatalog(List<string> issues)
        {
            Garage.PartCatalogDocument document;
            try
            {
                document = Garage.PartCatalogImporter.LoadFromStreamingAssets();
            }
            catch (System.Exception ex)
            {
                issues.Add($"PartCatalogImporter: failed to load the research corpus ({ex.Message})");
                return;
            }

            var parts = Garage.ResearchPartConverter.ConvertAll(document);
            var seenIds = new HashSet<string>();
            foreach (var part in parts)
            {
                if (!seenIds.Add(part.Id))
                {
                    issues.Add($"ResearchPartConverter: duplicate converted part id '{part.Id}'");
                }
                if (part.Price <= 0)
                {
                    issues.Add($"ResearchPartConverter: part '{part.Id}' has a non-positive price ({part.Price})");
                }
                if (part.TopSpeedDelta != 0 || part.AccelerationDelta != 0)
                {
                    issues.Add($"ResearchPartConverter: part '{part.Id}' has a nonzero performance delta -- should never happen by design");
                }
            }
        }
    }
}
