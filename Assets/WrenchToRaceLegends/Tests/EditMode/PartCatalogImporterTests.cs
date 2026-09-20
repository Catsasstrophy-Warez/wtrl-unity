using NUnit.Framework;
using WTRL.Garage;

namespace WTRL.Tests
{
    /// <summary>
    /// Verifies the batch importer against the REAL corpus file mirrored
    /// into `Assets/StreamingAssets/Corpus/master_parts_catalog.json`
    /// (copied from racinggame/ImportedVehicleCorpus/Content/
    /// Engineering/ -- the actual source of truth), not a synthetic
    /// fixture -- closes "no batch content importer connects the real
    /// JSON catalogs to the game".
    /// </summary>
    public class PartCatalogImporterTests
    {
        [Test]
        public void ImportsAllRealEntriesFamiliesAndGenerationsFromTheActualCorpusFile()
        {
            var document = PartCatalogImporter.LoadFromStreamingAssets();

            Assert.That(document.Schema, Is.EqualTo("wtrl.rev24.master-parts.v1"));
            Assert.That(document.Entries.Count, Is.EqualTo(document.EntryCount),
                "the deserialized entry count must match the document's own declared entryCount");
            Assert.That(document.Entries.Count, Is.EqualTo(1560));
            Assert.That(document.Families.Count, Is.EqualTo(30));
            Assert.That(document.Generations.Count, Is.EqualTo(9));
        }

        [Test]
        public void EveryEntryHasARealFamilyThatExistsInTheFamiliesList()
        {
            var document = PartCatalogImporter.LoadFromStreamingAssets();
            var familyIds = new System.Collections.Generic.HashSet<string>();
            foreach (var family in document.Families) familyIds.Add(family.Id);

            foreach (var entry in document.Entries)
            {
                Assert.That(familyIds, Does.Contain(entry.Family),
                    $"entry {entry.SpecificationId} references unknown family {entry.Family}");
            }
        }

        [Test]
        public void EveryEntryHasARealGenerationOrIsDeliberatelyUniversal()
        {
            // Found by an actual assertion failure, not designed up
            // front: some real entries (specificationId starting
            // "universal.") have an empty generationId -- a genuine
            // "applies across every generation" sentinel in the source
            // data, not a data-quality bug. Every non-empty generationId
            // must still be real.
            var document = PartCatalogImporter.LoadFromStreamingAssets();
            var generationIds = new System.Collections.Generic.HashSet<string>();
            foreach (var generation in document.Generations) generationIds.Add(generation.Id);

            foreach (var entry in document.Entries)
            {
                if (entry.GenerationId.Length == 0)
                {
                    Assert.That(entry.SpecificationId, Does.StartWith("universal."),
                        $"entry {entry.SpecificationId} has an empty generationId but isn't a universal part");
                    continue;
                }
                Assert.That(generationIds, Does.Contain(entry.GenerationId),
                    $"entry {entry.SpecificationId} references unknown generation {entry.GenerationId}");
            }
        }

        [Test]
        public void A1967EngineBlockFactoryEntryImportsWithItsRealFieldValues()
        {
            var document = PartCatalogImporter.LoadFromStreamingAssets();
            var entry = document.Entries.Find(e => e.SpecificationId == "hero_1967.engineblockbottomend.factory");

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.GenerationId, Is.EqualTo("hero_1967"));
            Assert.That(entry.Family, Is.EqualTo("EngineBlockBottomEnd"));
            Assert.That(entry.Origin, Is.EqualTo("OEM"));
            Assert.That(entry.Quality, Is.EqualTo("Standard"));
            Assert.That(entry.ResearchStatus, Is.EqualTo("research-required-for-exact-oem-values"));
        }

        [Test]
        public void ImportFromJsonIsPureAndIndependentOfFileIo()
        {
            const string json = @"{
                ""schema"": ""test-schema"",
                ""familyCount"": 1,
                ""generationCount"": 1,
                ""entryCount"": 1,
                ""families"": [ { ""id"": ""TestFamily"", ""displayName"": ""Test Family"", ""description"": ""d"" } ],
                ""generations"": [ { ""id"": ""gen1"", ""name"": ""Gen 1"", ""eraTag"": ""tag"" } ],
                ""entries"": [ { ""specificationId"": ""gen1.testfamily.factory"", ""engineeringId"": ""ENG-1"",
                    ""generationId"": ""gen1"", ""generationName"": ""Gen 1"", ""family"": ""TestFamily"",
                    ""familyDisplayName"": ""Test Family"", ""subtype"": ""factory"", ""displayName"": ""x"",
                    ""description"": ""y"", ""origin"": ""OEM"", ""quality"": ""Standard"", ""eraTag"": ""tag"",
                    ""researchStatus"": ""synthetic-engineering-baseline"", ""variantLevel"": 0,
                    ""dependencies"": [], ""consequences"": [] } ]
            }";

            var document = PartCatalogImporter.ImportFromJson(json);

            Assert.That(document.Entries.Count, Is.EqualTo(1));
            Assert.That(document.Entries[0].SpecificationId, Is.EqualTo("gen1.testfamily.factory"));
        }
    }
}
