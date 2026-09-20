using System.Collections.Generic;

namespace WTRL.Garage
{
    /// <summary>
    /// A faithful C# port of the real 35 build-recipe entries from the
    /// original Swift game (`SwiftRacer/Sources/WTRLCore/Content
    /// /CanonicalContent.swift`'s `buildRecipes` array), closing "only 1
    /// of 35 spec'd recipes exists" from the world-content gap audit.
    /// This is a straight transcription, not new content -- every id,
    /// tier, weight-to-power band, and required-part field below is
    /// copied byte-for-byte from that source, including its own source
    /// comments (condensed here) about which bands are open modeling
    /// decisions versus directly-sourced real horsepower/mass figures.
    ///
    /// NONE of the 6 non-hero-1965 vehicle IDs referenced here
    /// (`hero-mid70s`, `hero-late80s`, `hero-mid90s`, `hero-early00s`,
    /// `hero-mid10s`, `hero-2022`) have a real `VehicleDefinitionAsset`
    /// built in this project yet -- only `hero-1965` (via
    /// `HeroContentBuilder`) and the rival `MarshGen1` exist. Porting
    /// the recipe DATA doesn't require the vehicle assets to already
    /// exist (`BuildRecipeEvaluator` only checks weight-to-power ratio
    /// and required-part-type against whatever `VehicleDefinitionAsset`
    /// a caller supplies), but a recipe here can't be practically
    /// "satisfied" in-game until its vehicle's real content is built --
    /// an honest, documented follow-on gap, not fixed by this port.
    /// </summary>
    public static class CanonicalBuildRecipes
    {
        public static IReadOnlyList<BuildRecipeDefinition> All { get; } = new[]
        {
            // ---- 1965 (base 1450kg, 164hp baseline / 195-225hp base-spec
            // / 271hp real HiPo) -- the one generation with a full playable
            // spec to build recipes against when this ladder was written. ----
            new BuildRecipeDefinition("hero1965-driveway-special", "hero-1965", "base",
                "Driveway Special", 7.5, 8.84, "Driveway Special"),
            new BuildRecipeDefinition("hero1965-full-compression", "hero-1965", "performance",
                "Full Compression", 6.44, 7.44, "Full Compression") { RequiredDifferentialType = "lsd" },
            new BuildRecipeDefinition("hero1965-hipo-spec", "hero-1965", "factoryHot",
                "Hi-Po Spec", 5.2, 5.5, "Hi-Po Spec")
                { RequiredDifferentialType = "lsdRace", UnlockedLiveryId = "hero1965-hipo-stripe" },
            new BuildRecipeDefinition("hero1965-homologation-run", "hero-1965", "homologation",
                "Homologation Run", 3.8, 4.1, "Homologation Run") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero1965-paxton-special", "hero-1965", "tunerHalo",
                "Paxton Special", 3.5, 3.7, "Paxton Special")
                { RequiredDifferentialType = "lsdRace", UnlockedLiveryId = "hero1965-halo-livery" },

            // ---- Mid-70s (mass 1580kg) -- compressed ladder: base and
            // performance share the same real 139-140hp figure (no higher
            // factory number exists for this era). Homologation's forced-
            // induction path is an open modeling decision (source doc
            // Sec.1.4: 1965-72 period-supercharger sourcing may or may not
            // cover this generation) -- banded here assuming the
            // aftermarket-turbo path pending that decision, not resolved. ----
            new BuildRecipeDefinition("hero75-driveway-special", "hero-mid70s", "base",
                "Driveway Special", 10.5, 11.29, "Driveway Special"),
            new BuildRecipeDefinition("hero75-full-compression", "hero-mid70s", "performance",
                "Full Compression", 11.0, 11.29, "Full Compression") { RequiredDifferentialType = "lsd" },
            new BuildRecipeDefinition("hero75-federal-spec", "hero-mid70s", "factoryHot",
                "Federal Spec", 10.9, 11.1, "Federal Spec") { RequiredDifferentialType = "lsd" },
            new BuildRecipeDefinition("hero75-homologation-run", "hero-mid70s", "homologation",
                "Homologation Run", 8.0, 8.5, "Homologation Run") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero75-paxton-special", "hero-mid70s", "tunerHalo",
                "Paxton Special", 7.3, 7.6, "Paxton Special")
                { RequiredDifferentialType = "lsdRace", UnlockedLiveryId = "hero75-halo-livery" },

            // ---- Late 80s (mass 1500kg) -- 195-225hp real band, T5
            // 5-speed era. ----
            new BuildRecipeDefinition("hero88-driveway-special", "hero-late80s", "base",
                "Driveway Special", 7.4, 7.69, "Driveway Special"),
            new BuildRecipeDefinition("hero88-full-compression", "hero-late80s", "performance",
                "Full Compression", 6.67, 7.0, "Full Compression") { RequiredDifferentialType = "lsd" },
            new BuildRecipeDefinition("hero88-5-0-ho-spec", "hero-late80s", "factoryHot",
                "5.0 HO Spec", 6.5, 6.67, "5.0 HO Spec") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero88-homologation-run", "hero-late80s", "homologation",
                "Homologation Run", 4.8, 5.0, "Homologation Run") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero88-t5-special", "hero-late80s", "tunerHalo",
                "T5 Special", 4.4, 4.6, "T5 Special")
            {
                RequiredDifferentialType = "lsdRace", RequiredTransmissionId = "t5-late80s",
                UnlockedLiveryId = "hero88-halo-livery",
            },

            // ---- Mid-90s (mass 1540kg) -- real two-trim generation:
            // 215-265hp base 2V, 305hp/300lb-ft 4V DOHC (the real SVT
            // Cobra spec) as factory hot -- the strongest historically-
            // grounded recipe band in the whole set. ----
            new BuildRecipeDefinition("hero95-driveway-special", "hero-mid90s", "base",
                "Driveway Special", 6.9, 7.16, "Driveway Special"),
            new BuildRecipeDefinition("hero95-full-compression", "hero-mid90s", "performance",
                "Full Compression", 5.81, 6.2, "Full Compression") { RequiredDifferentialType = "lsd" },
            new BuildRecipeDefinition("hero95-cobra-spec", "hero-mid90s", "factoryHot",
                "Cobra Spec", 4.9, 5.1, "Cobra Spec") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero95-homologation-run", "hero-mid90s", "homologation",
                "Homologation Run", 3.9, 4.1, "Homologation Run") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero95-t45-special", "hero-mid90s", "tunerHalo",
                "T45 Special", 3.6, 3.8, "T45 Special")
            {
                RequiredDifferentialType = "lsdRace", RequiredTransmissionId = "t45-mid90s",
                UnlockedLiveryId = "hero95-halo-livery",
            },

            // ---- Early 2000s (mass 1660kg) -- 300-315hp 3V, TR-3650
            // 5-speed era. ----
            new BuildRecipeDefinition("hero04-driveway-special", "hero-early00s", "base",
                "Driveway Special", 5.3, 5.53, "Driveway Special"),
            new BuildRecipeDefinition("hero04-full-compression", "hero-early00s", "performance",
                "Full Compression", 5.0, 5.27, "Full Compression") { RequiredDifferentialType = "lsd" },
            new BuildRecipeDefinition("hero04-3v-spec", "hero-early00s", "factoryHot",
                "3V Spec", 4.9, 5.0, "3V Spec") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero04-homologation-run", "hero-early00s", "homologation",
                "Homologation Run", 4.0, 4.2, "Homologation Run") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero04-tr3650-special", "hero-early00s", "tunerHalo",
                "TR-3650 Special", 3.7, 3.9, "TR-3650 Special")
            {
                RequiredDifferentialType = "lsdRace", RequiredTransmissionId = "tr3650-early00s",
                UnlockedLiveryId = "hero04-halo-livery",
            },

            // ---- Mid-2010s (mass 1680kg) -- real running change: 435hp at
            // 2015 debut, 460hp from the real 2018 12.0:1-compression
            // revision. First generation since 1965 where the homologation
            // rung can be factory-supercharged rather than aftermarket-
            // turbo (source doc Sec.5.4). ----
            new BuildRecipeDefinition("hero15-driveway-special", "hero-mid10s", "base",
                "Driveway Special", 3.7, 3.86, "Driveway Special"),
            new BuildRecipeDefinition("hero15-full-compression", "hero-mid10s", "performance",
                "Full Compression", 3.5, 3.65, "Full Compression") { RequiredDifferentialType = "lsd" },
            new BuildRecipeDefinition("hero15-compression-bump-spec", "hero-mid10s", "factoryHot",
                "Compression Bump Spec", 3.45, 3.55, "Compression Bump Spec") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero15-homologation-run", "hero-mid10s", "homologation",
                "Homologation Run", 2.9, 3.1, "Homologation Run") { RequiredDifferentialType = "lsdRace" },
            new BuildRecipeDefinition("hero15-mt82-special", "hero-mid10s", "tunerHalo",
                "MT82 Special", 2.7, 2.9, "MT82 Special")
            {
                RequiredDifferentialType = "lsdRace", RequiredTransmissionId = "mt82-mid10s",
                UnlockedLiveryId = "hero15-halo-livery",
            },

            // ---- 2022 (mass 1890kg) -- the real three-trim generation
            // (450/470/760hp). Deliberately not forced into five even
            // rungs: Voodoo and Predator ARE the flat-plane and
            // supercharged trims themselves, with real manual-only /
            // automatic-only exclusivity (source doc Sec.2.7), and
            // 10-Speed Special is the base engine on the real 10-speed
            // auto rather than an invented fourth power tier. ----
            new BuildRecipeDefinition("hero22-driveway-special", "hero-2022", "base",
                "Driveway Special", 4.0, 4.2, "Driveway Special"),
            new BuildRecipeDefinition("hero22-full-compression", "hero-2022", "performance",
                "Full Compression", 3.95, 4.1, "Full Compression") { RequiredDifferentialType = "lsd" },
            new BuildRecipeDefinition("hero22-voodoo-spec", "hero-2022", "flatPlane",
                "Voodoo Spec", 3.9, 4.05, "Voodoo Spec")
            {
                RequiredDifferentialType = "lsdRace", RequiredTransmissionId = "tr3160-2022m",
                RequiredCrankType = "flatPlane",
            },
            new BuildRecipeDefinition("hero22-predator-spec", "hero-2022", "supercharged",
                "Predator Spec", 2.4, 2.55, "Predator Spec")
            {
                RequiredDifferentialType = "lsdRace", UnlockedLiveryId = "hero22-predator-livery",
                RequiredTransmissionId = "7d",
            },
            new BuildRecipeDefinition("hero22-10-speed-special", "hero-2022", "base",
                "10-Speed Special", 4.0, 4.2, "10-Speed Special")
                { RequiredDifferentialType = "lsd", RequiredTransmissionId = "10a" },
        };
    }
}
