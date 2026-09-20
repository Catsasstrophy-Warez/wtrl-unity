using NUnit.Framework;
using WTRL.Core;

namespace WTRL.Tests
{
    /// <summary>Tests for the project's first localization infrastructure
    /// -- closes "no localization" from the Milestone M8 gap audit. See
    /// LocalizationTable.cs's own doc comment for the honest scope
    /// limit: only English is populated, only ResultsScreen has been
    /// migrated to call through it.</summary>
    public class LocalizationTableTests
    {
        [TearDown]
        public void ResetActiveLocale() => LocalizationTable.ActiveLocale = LocalizationTable.DefaultLocale;

        [Test]
        public void GetReturnsTheRealEnglishStringForAKnownKey()
        {
            Assert.That(LocalizationTable.Get("results.title"), Is.EqualTo("Results"));
            Assert.That(LocalizationTable.Get("results.continue"), Is.EqualTo("Continue"));
        }

        [Test]
        public void GetFallsBackToTheKeyItselfForAnUnknownKeyRatherThanThrowing()
        {
            Assert.That(LocalizationTable.Get("some.key.nobody.registered"), Is.EqualTo("some.key.nobody.registered"));
        }

        [Test]
        public void GetFallsBackToEnglishWhenTheActiveLocaleIsMissingAKey()
        {
            LocalizationTable.RegisterLocale("fr", new System.Collections.Generic.Dictionary<string, string>
            {
                ["results.title"] = "Résultats",
                // "results.continue" deliberately NOT translated -- a
                // real, common partial-translation scenario.
            });
            LocalizationTable.ActiveLocale = "fr";

            Assert.That(LocalizationTable.Get("results.title"), Is.EqualTo("Résultats"));
            Assert.That(LocalizationTable.Get("results.continue"), Is.EqualTo("Continue"));
        }

        [Test]
        public void SettingActiveLocaleToNullOrEmptyResetsToDefault()
        {
            LocalizationTable.ActiveLocale = "fr";
            LocalizationTable.ActiveLocale = null;
            Assert.That(LocalizationTable.ActiveLocale, Is.EqualTo(LocalizationTable.DefaultLocale));
        }
    }
}
