using System.Collections.Generic;

namespace WTRL.Garage
{
    /// <summary>
    /// Batch-converts `PartCatalogImporter`'s real 1,560-entry research
    /// records into gameplay-usable `PartDefinition`s -- closes "no
    /// batch importer connects the real JSON catalogs to the game" from
    /// the world-content gap audit, for the half of that gap this can
    /// honestly do without a human.
    ///
    /// HONEST SCOPE, stated as plainly as possible: the research corpus
    /// has NO price, reputation-gate, or performance-delta data for any
    /// entry (confirmed by reading real entries directly -- see
    /// `PartCatalogImporter.cs`'s own doc comment). There is no way to
    /// derive real gameplay balance numbers from data that doesn't
    /// exist. What this converter does instead:
    ///
    /// - `Price`/`ReputationRequired` are a DETERMINISTIC FORMULA over
    ///   real categorical fields the corpus DOES have (`Origin`,
    ///   `Quality`, `VariantLevel`) -- not random, not hand-tuned per
    ///   part, and explicitly flagged here as a PLACEHOLDER pricing
    ///   curve, not researched or balance-tested content. A real
    ///   pricing pass requires human game-design judgment this
    ///   environment cannot supply.
    /// - `TopSpeedDelta`/`AccelerationDelta` are left at EXACTLY ZERO
    ///   for every converted part. Inventing nonzero performance deltas
    ///   with no real physics basis would be fabricating gameplay
    ///   balance, which is exactly the discipline this project's whole
    ///   "flag invented vs. sourced" approach exists to prevent -- a
    ///   part that does nothing mechanically yet is more honest than
    ///   one with a made-up effect.
    /// - Converted parts are NOT wired into `VehicleConfigurationResolver
    ///   .Resolve`'s switch statement -- that resolver's whole point is
    ///   that every recognized part id has a real, intentional
    ///   mechanical effect; adding 1,560 entries there with invented
    ///   effects would defeat that guarantee. They're real, priced,
    ///   named, browsable inventory (a real shop catalog a UI could
    ///   list) that mechanically no-ops when installed, same as any
    ///   currently-unrecognized part id already does.
    /// </summary>
    public static class ResearchPartConverter
    {
        private static int BasePriceForOrigin(string origin) => origin switch
        {
            "OEM" => 200,
            "OEMReplacement" => 350,
            "Aftermarket" => 500,
            _ => 300,
        };

        public static PartDefinition Convert(ResearchPartRecord record)
        {
            var basePrice = BasePriceForOrigin(record.Origin);
            var variantMultiplier = 1.0 + record.VariantLevel * 0.5;
            var qualityMultiplier = record.Quality == "Premium" ? 1.3 : 1.0;
            var price = (int)(basePrice * variantMultiplier * qualityMultiplier);
            var reputationRequired = record.VariantLevel * 5;

            return new PartDefinition(
                id: record.SpecificationId,
                category: record.FamilyDisplayName,
                name: record.DisplayName,
                price: price,
                reputationRequired: reputationRequired,
                topSpeedDelta: 0,
                accelerationDelta: 0);
        }

        public static IReadOnlyList<PartDefinition> ConvertAll(PartCatalogDocument document)
        {
            var result = new List<PartDefinition>(document.Entries.Count);
            foreach (var entry in document.Entries)
            {
                result.Add(Convert(entry));
            }
            return result;
        }
    }
}
