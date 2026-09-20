using System.Collections.Generic;
using NUnit.Framework;
using WTRL.Garage;

namespace WTRL.Tests
{
    /// <summary>Tests for the real batch converter from research records
    /// to gameplay `PartDefinition`s -- see `ResearchPartConverter.cs`'s
    /// own doc comment for the honest scope limit: Price/
    /// ReputationRequired are a deterministic placeholder formula (not
    /// researched), TopSpeedDelta/AccelerationDelta are always exactly
    /// zero (no invented performance effects).</summary>
    public class ResearchPartConverterTests
    {
        private static ResearchPartRecord MakeRecord(string id, string origin, string quality, int variantLevel) => new()
        {
            SpecificationId = id,
            FamilyDisplayName = "Test Family",
            DisplayName = "Test Part",
            Origin = origin,
            Quality = quality,
            VariantLevel = variantLevel,
        };

        [Test]
        public void ConvertNeverInventsAPerformanceEffect()
        {
            var part = ResearchPartConverter.Convert(MakeRecord("x", "Aftermarket", "Premium", 5));

            Assert.That(part.TopSpeedDelta, Is.EqualTo(0));
            Assert.That(part.AccelerationDelta, Is.EqualTo(0));
        }

        [Test]
        public void ConvertPreservesIdCategoryAndName()
        {
            var record = MakeRecord("hero_1967.ignition.factory", "OEM", "Standard", 0);
            var part = ResearchPartConverter.Convert(record);

            Assert.That(part.Id, Is.EqualTo("hero_1967.ignition.factory"));
            Assert.That(part.Category, Is.EqualTo("Test Family"));
            Assert.That(part.Name, Is.EqualTo("Test Part"));
        }

        [Test]
        public void HigherVariantLevelProducesAHigherPriceAndReputationGate()
        {
            var low = ResearchPartConverter.Convert(MakeRecord("a", "OEM", "Standard", 0));
            var high = ResearchPartConverter.Convert(MakeRecord("b", "OEM", "Standard", 5));

            Assert.That(high.Price, Is.GreaterThan(low.Price));
            Assert.That(high.ReputationRequired, Is.GreaterThan(low.ReputationRequired));
        }

        [Test]
        public void PremiumQualityCostsMoreThanStandardAtTheSameVariantLevel()
        {
            var standard = ResearchPartConverter.Convert(MakeRecord("a", "OEM", "Standard", 2));
            var premium = ResearchPartConverter.Convert(MakeRecord("b", "OEM", "Premium", 2));

            Assert.That(premium.Price, Is.GreaterThan(standard.Price));
        }

        [Test]
        public void AftermarketPartsCostMoreThanOemAtTheSameVariantLevel()
        {
            var oem = ResearchPartConverter.Convert(MakeRecord("a", "OEM", "Standard", 2));
            var aftermarket = ResearchPartConverter.Convert(MakeRecord("b", "Aftermarket", "Standard", 2));

            Assert.That(aftermarket.Price, Is.GreaterThan(oem.Price));
        }

        [Test]
        public void EveryPriceIsPositive()
        {
            // Even the cheapest real combination (base-tier OEM,
            // Standard quality, variant level 0) must still cost
            // something -- a free part would be a real content bug.
            var part = ResearchPartConverter.Convert(MakeRecord("a", "OEM", "Standard", 0));
            Assert.That(part.Price, Is.GreaterThan(0));
        }

        [Test]
        public void ConvertingTheRealFullCorpusProducesOneUniquelyIdentifiedPartPerEntry()
        {
            var document = PartCatalogImporter.LoadFromStreamingAssets();
            var parts = ResearchPartConverter.ConvertAll(document);

            Assert.That(parts.Count, Is.EqualTo(document.EntryCount));

            var seenIds = new HashSet<string>();
            foreach (var part in parts)
            {
                Assert.That(seenIds.Add(part.Id), Is.True, $"duplicate converted part id: {part.Id}");
                Assert.That(part.Price, Is.GreaterThan(0), $"part {part.Id} has a non-positive price");
                Assert.That(part.TopSpeedDelta, Is.EqualTo(0), $"part {part.Id} should have no invented performance effect");
                Assert.That(part.AccelerationDelta, Is.EqualTo(0), $"part {part.Id} should have no invented performance effect");
            }
        }
    }
}
