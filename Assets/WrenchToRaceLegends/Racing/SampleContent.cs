using System;
using System.Collections.Generic;

namespace WTRL.Racing
{
    /// <summary>
    /// The project's first authored AI racing-line content, paired with
    /// WTRL.World.SampleContent's track definitions. WTRL.Racing has no
    /// dependency on WTRL.World (and shouldn't gain one just for this --
    /// confirmed the hard way: an earlier version of this file referenced
    /// `World.SampleContent.FoundryRowCircuitId` directly and failed to
    /// compile in the real Unity Editor with CS0103, since Racing's
    /// asmdef has no World reference; the throwaway dotnet-test method
    /// missed this because it flattens every assembly into one flat
    /// folder, hiding real cross-assembly reference boundaries). Fixed by
    /// duplicating each track id as a literal string instead -- the same
    /// explicit-reference-by-id discipline `WTRL.RPG.BuildRecipeProgress`
    /// already uses to avoid a `WTRL.Garage` dependency. Every id below
    /// must match its `WTRL.World.SampleContent` counterpart exactly --
    /// `SampleContentTests` guards several of these pairs.
    /// </summary>
    public static class SampleContent
    {
        public const string FoundryRowCircuitTrackId = "foundry-row-circuit";
        public const string CutbackTriOvalTrackId = "cutback-tri-oval";
        public const string LongbowSpeedwayTrackId = "longbow-speedway";
        public const string HighbankSuperspeedwayTrackId = "highbank-superspeedway";
        public const string WhisperwoodForestCircuitTrackId = "whisperwood-forest-circuit";
        public const string CliffsideCoastalCircuitTrackId = "cliffside-coastal-circuit";
        public const string IroncladTechnicalCircuitTrackId = "ironclad-technical-circuit";

        public static TrackLineDefinition FoundryRowCircuitLine() => new(
            "foundry-row-circuit-line", FoundryRowCircuitTrackId, new[]
            {
                new TrackNode(0, 0, targetSpeedMps: 45),
                new TrackNode(200, 0, targetSpeedMps: 45),
                new TrackNode(220, 20, targetSpeedMps: 18, brakingWeight: 0.8),
                new TrackNode(220, 80, targetSpeedMps: 22),
                new TrackNode(200, 100, targetSpeedMps: 40),
                new TrackNode(0, 100, targetSpeedMps: 40),
                new TrackNode(-20, 80, targetSpeedMps: 18, brakingWeight: 0.8),
                new TrackNode(-20, 20, targetSpeedMps: 22),
            });

        // ---- Ovals: generated, not hand-typed, from a stadium-shape
        // (2 straights + 2 semicircular ends) parametric loop. Real oval
        // tracks are far more precisely engineered than this, but a
        // procedural loop is a more honest way to produce "banked oval
        // shape" content than hand-placing dozens of node coordinates
        // that would only look precise. ----

        public static TrackLineDefinition CutbackTriOvalLine() =>
            BuildOvalLine(CutbackTriOvalTrackId, straightLengthM: 290, turnRadiusM: 100,
                straightSpeedMps: 55, turnSpeedMps: 35);

        public static TrackLineDefinition LongbowSpeedwayLine() =>
            BuildOvalLine(LongbowSpeedwayTrackId, straightLengthM: 579, turnRadiusM: 200,
                straightSpeedMps: 70, turnSpeedMps: 45);

        public static TrackLineDefinition HighbankSuperspeedwayLine() =>
            BuildOvalLine(HighbankSuperspeedwayTrackId, straightLengthM: 1069, turnRadiusM: 300,
                straightSpeedMps: 85, turnSpeedMps: 60);

        // ---- Original road-course archetypes (see WTRL.World
        // .SampleContent's class doc for the naming caveat). Elevation
        // isn't modeled by anything in this project, so "blind crest"/
        // "cliff drop" are only conveyed through node shape and speed,
        // not actual height data -- there is no Y coordinate here or
        // anywhere else this project's 2D (X/Z) track-line model. ----

        /// <summary>Long, low-sightline loop with a wide, sweeping shape
        /// -- the widest average node spacing of the three, since blind
        /// crests are represented here only as "can't see the apex
        /// coming" via lower cornering speed relative to the loop's
        /// scale, not any real elevation change.</summary>
        public static TrackLineDefinition WhisperwoodForestCircuitLine() => new(
            "whisperwood-forest-circuit-line", WhisperwoodForestCircuitTrackId, new[]
            {
                new TrackNode(0, 0, targetSpeedMps: 50),
                new TrackNode(320, 60, targetSpeedMps: 42, brakingWeight: 0.4),
                new TrackNode(480, 220, targetSpeedMps: 28, brakingWeight: 0.7),
                new TrackNode(420, 420, targetSpeedMps: 36),
                new TrackNode(200, 520, targetSpeedMps: 30, brakingWeight: 0.5),
                new TrackNode(-60, 480, targetSpeedMps: 44),
                new TrackNode(-260, 340, targetSpeedMps: 32, brakingWeight: 0.6),
                new TrackNode(-300, 140, targetSpeedMps: 38),
                new TrackNode(-160, 20, targetSpeedMps: 40),
            });

        /// <summary>Right-left sequence into one steep single drop
        /// (represented as its sharpest single corner, node index 4 --
        /// the "signature moment" the corpus describes) then a
        /// cliff-hugging return leg.</summary>
        public static TrackLineDefinition CliffsideCoastalCircuitLine() => new(
            "cliffside-coastal-circuit-line", CliffsideCoastalCircuitTrackId, new[]
            {
                new TrackNode(0, 0, targetSpeedMps: 48),
                new TrackNode(180, -40, targetSpeedMps: 34, brakingWeight: 0.5), // right
                new TrackNode(340, 40, targetSpeedMps: 34, brakingWeight: 0.5), // left
                new TrackNode(420, 220, targetSpeedMps: 30),
                new TrackNode(360, 380, targetSpeedMps: 14, brakingWeight: 0.9), // the signature drop
                new TrackNode(160, 400, targetSpeedMps: 38),
                new TrackNode(-60, 320, targetSpeedMps: 40),
                new TrackNode(-140, 140, targetSpeedMps: 36),
            });

        /// <summary>Tight, angular, more corners per meter than the
        /// other two -- the corpus's "crossing/figure-eight pattern,
        /// tight barrier-close infield." A true figure-eight (the line
        /// crossing itself) isn't representable by
        /// `TrackAiDriver.Perceive`'s nearest-node model (it assumes a
        /// simple loop), so this approximates the *feel* of a tight
        /// technical infield without an actual self-intersecting
        /// path.</summary>
        public static TrackLineDefinition IroncladTechnicalCircuitLine() => new(
            "ironclad-technical-circuit-line", IroncladTechnicalCircuitTrackId, new[]
            {
                new TrackNode(0, 0, targetSpeedMps: 38),
                new TrackNode(140, 10, targetSpeedMps: 20, brakingWeight: 0.7),
                new TrackNode(160, 100, targetSpeedMps: 24),
                new TrackNode(90, 160, targetSpeedMps: 16, brakingWeight: 0.8),
                new TrackNode(10, 130, targetSpeedMps: 22),
                new TrackNode(-70, 180, targetSpeedMps: 16, brakingWeight: 0.8),
                new TrackNode(-150, 110, targetSpeedMps: 24),
                new TrackNode(-140, 20, targetSpeedMps: 20, brakingWeight: 0.6),
            });

        /// <summary>Generates a stadium-shape loop (2 straights + 2
        /// semicircular ends) centered on the origin, long axis along X.
        /// <paramref name="nodesPerTurn"/> controls how many waypoints
        /// approximate each semicircle -- more nodes means smoother AI
        /// steering through the turn at the cost of more perception
        /// checks per lap.</summary>
        private static TrackLineDefinition BuildOvalLine(string trackId, double straightLengthM, double turnRadiusM,
            double straightSpeedMps, double turnSpeedMps, int nodesPerTurn = 6)
        {
            var half = straightLengthM / 2;
            var nodes = new List<TrackNode>
            {
                new(-half, -turnRadiusM, straightSpeedMps),
                new(half, -turnRadiusM, straightSpeedMps),
            };

            for (var i = 1; i < nodesPerTurn; i++)
            {
                var t = (double)i / nodesPerTurn;
                var angle = -Math.PI / 2 + t * Math.PI;
                nodes.Add(new TrackNode(half + turnRadiusM * Math.Cos(angle), turnRadiusM * Math.Sin(angle),
                    turnSpeedMps, brakingWeight: 0.3));
            }

            nodes.Add(new TrackNode(half, turnRadiusM, straightSpeedMps));
            nodes.Add(new TrackNode(-half, turnRadiusM, straightSpeedMps));

            for (var i = 1; i < nodesPerTurn; i++)
            {
                var t = (double)i / nodesPerTurn;
                var angle = Math.PI / 2 + t * Math.PI;
                nodes.Add(new TrackNode(-half + turnRadiusM * Math.Cos(angle), turnRadiusM * Math.Sin(angle),
                    turnSpeedMps, brakingWeight: 0.3));
            }

            return new TrackLineDefinition(trackId + "-line", trackId, nodes);
        }
    }
}
