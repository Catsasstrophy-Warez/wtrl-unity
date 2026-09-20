using System.Collections.Generic;

namespace WTRL.Core
{
    /// <summary>
    /// The project's first real localization infrastructure -- closes
    /// "no localization" from the Milestone M8 gap audit. This is
    /// deliberately placed in `WTRL.Core` (the zero-dependency root
    /// assembly) so any assembly can key its user-facing strings through
    /// it without creating a new dependency.
    ///
    /// HONEST SCOPE: this ports real UI strings that already exist as
    /// hardcoded literals in `WTRL.UI`'s screens (`ResultsScreen`,
    /// `GarageScreen`, `DynoScreen`, `TelemetryHud`) into one keyed
    /// English table plus a lookup API that already supports adding more
    /// locales -- but only English is populated. Migrating every UI
    /// screen's hardcoded strings to call through `Get` is real, ongoing
    /// follow-on work (this pass migrates `ResultsScreen` as the
    /// concrete example; the other screens are NOT touched here), not
    /// claimed as done. There is no translation content for any
    /// non-English locale -- that requires an actual human translator,
    /// not something this pass can produce honestly.
    /// </summary>
    public static class LocalizationTable
    {
        public const string DefaultLocale = "en";

        private static readonly Dictionary<string, Dictionary<string, string>> Tables = new()
        {
            [DefaultLocale] = new Dictionary<string, string>
            {
                ["results.title"] = "Results",
                ["results.continue"] = "Continue",
                ["results.classifiedTime"] = "Classified time",
                ["results.laps"] = "Laps",
                ["results.bestLap"] = "Best lap",
                ["results.penalties"] = "Penalties",
                ["garage.title"] = "Garage",
                ["dyno.title"] = "Dyno",
                ["dyno.finalDrive"] = "Final drive",
                ["dyno.tirePressure"] = "Tire pressure",
                ["dyno.nitrous"] = "Nitrous",
                ["dyno.peakPower"] = "Peak power",
                ["dyno.peakTorque"] = "Peak torque",
            },
        };

        private static string _activeLocale = DefaultLocale;

        /// <summary>Currently active locale code. Setting an unpopulated
        /// locale is allowed (so a future locale table can be added
        /// without a code change here) -- lookups against it fall back
        /// to English via <see cref="Get"/>, never throw.</summary>
        public static string ActiveLocale
        {
            get => _activeLocale;
            set => _activeLocale = string.IsNullOrEmpty(value) ? DefaultLocale : value;
        }

        /// <summary>Registers or replaces a locale's whole string table --
        /// how a real translation pass would add a new locale without
        /// modifying this file.</summary>
        public static void RegisterLocale(string locale, IReadOnlyDictionary<string, string> strings)
        {
            var table = new Dictionary<string, string>();
            foreach (var kvp in strings) table[kvp.Key] = kvp.Value;
            Tables[locale] = table;
        }

        /// <summary>Looks up <paramref name="key"/> in the active locale;
        /// falls back to English if missing from the active locale, and
        /// to the literal key itself if missing from English too (a
        /// visibly-wrong-but-never-crashing fallback, matching this
        /// project's "never silently guess" discipline applied to
        /// missing content instead of missing behavior).</summary>
        public static string Get(string key)
        {
            if (Tables.TryGetValue(_activeLocale, out var activeTable) && activeTable.TryGetValue(key, out var value))
            {
                return value;
            }
            if (Tables.TryGetValue(DefaultLocale, out var defaultTable) && defaultTable.TryGetValue(key, out var fallback))
            {
                return fallback;
            }
            return key;
        }
    }
}
