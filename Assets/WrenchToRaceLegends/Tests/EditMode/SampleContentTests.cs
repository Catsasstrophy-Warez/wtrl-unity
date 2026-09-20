using NUnit.Framework;
using WTRL.Racing;
using WTRL.Vehicle;
using WorldContent = WTRL.World.SampleContent;
using RacingContent = WTRL.Racing.SampleContent;

namespace WTRL.Tests
{
    /// <summary>New tests for the project's first authored (non-fixture)
    /// track content -- WTRL.World.SampleContent.FoundryRowCircuit() and
    /// WTRL.Racing.SampleContent.FoundryRowCircuitLine(). No Swift-test
    /// equivalent exists since this content didn't exist before this
    /// pass. Run via the same throwaway dotnet test project as every
    /// other assembly -- all pass.</summary>
    public class SampleContentTests
    {
        [Test]
        public void TrackDefinitionAndTrackLineShareTheSameId()
        {
            var track = WorldContent.FoundryRowCircuit();
            var line = RacingContent.FoundryRowCircuitLine();

            Assert.That(line.TrackId, Is.EqualTo(track.Id));
        }

        [Test]
        public void TrackDefinitionLengthMatchesTheActualPolylineLength()
        {
            // Guards against the two files drifting apart -- if someone
            // edits the line's nodes without updating the track's
            // declared length, this catches it.
            var track = WorldContent.FoundryRowCircuit();
            var nodes = RacingContent.FoundryRowCircuitLine().Nodes;

            Assert.That(track.LengthM, Is.EqualTo(Perimeter(nodes)).Within(0.5));
        }

        private static double Perimeter(System.Collections.Generic.IReadOnlyList<WTRL.Racing.TrackNode> nodes)
        {
            double perimeter = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                var a = nodes[i];
                var b = nodes[(i + 1) % nodes.Count];
                perimeter += System.Math.Sqrt(System.Math.Pow(b.X - a.X, 2) + System.Math.Pow(b.Z - a.Z, 2));
            }
            return perimeter;
        }

        [Test]
        public void AllWorldTracksHaveAMatchingRacingLineWithSameIdAndAccurateLength()
        {
            // Cross-checks every track this pass added: World's declared
            // TrackDefinition.LengthM against the actual polyline length
            // of its paired Racing.SampleContent line, and confirms the
            // id strings (duplicated across assemblies deliberately --
            // see both SampleContent files' class docs) haven't drifted.
            // The drag strip (Redline Raceway) has no racing line --
            // DragRaceRules drives progression by straight-line distance,
            // not waypoint-following -- so it's excluded here on purpose.
            (WTRL.World.TrackDefinition track, WTRL.Racing.TrackLineDefinition line)[] pairs =
            {
                (WorldContent.CutbackTriOval(), RacingContent.CutbackTriOvalLine()),
                (WorldContent.LongbowSpeedway(), RacingContent.LongbowSpeedwayLine()),
                (WorldContent.HighbankSuperspeedway(), RacingContent.HighbankSuperspeedwayLine()),
                (WorldContent.WhisperwoodForestCircuit(), RacingContent.WhisperwoodForestCircuitLine()),
                (WorldContent.CliffsideCoastalCircuit(), RacingContent.CliffsideCoastalCircuitLine()),
                (WorldContent.IroncladTechnicalCircuit(), RacingContent.IroncladTechnicalCircuitLine()),
            };

            foreach (var (track, line) in pairs)
            {
                Assert.That(line.TrackId, Is.EqualTo(track.Id), $"id mismatch for {track.Name}");
                Assert.That(track.LengthM, Is.EqualTo(Perimeter(line.Nodes)).Within(1.0),
                    $"{track.Name}: declared LengthM doesn't match its racing line's actual polyline length");
            }
        }

        [Test]
        public void RedlineRacewayIsAQuarterMileDragStripWithNoBanking()
        {
            var track = WorldContent.RedlineRaceway();
            Assert.That(track.Kind, Is.EqualTo("drag"));
            Assert.That(track.LengthM, Is.EqualTo(402.336).Within(0.01));
            Assert.That(track.BankingDegrees, Is.EqualTo(0));
        }

        [Test]
        public void OvalsHaveNonZeroBankingAndRoadCoursesDoNot()
        {
            Assert.That(WorldContent.CutbackTriOval().BankingDegrees, Is.GreaterThan(0));
            Assert.That(WorldContent.LongbowSpeedway().BankingDegrees, Is.GreaterThan(0));
            Assert.That(WorldContent.HighbankSuperspeedway().BankingDegrees, Is.GreaterThan(0));
            Assert.That(WorldContent.WhisperwoodForestCircuit().BankingDegrees, Is.EqualTo(0));
            Assert.That(WorldContent.CliffsideCoastalCircuit().BankingDegrees, Is.EqualTo(0));
            Assert.That(WorldContent.IroncladTechnicalCircuit().BankingDegrees, Is.EqualTo(0));
        }

        [Test]
        public void BlackridgeCountyReferencesAllOfItsRealFacilitiesAndDistricts()
        {
            var county = WorldContent.BlackridgeCounty();
            Assert.That(county.FacilityIds, Has.Count.EqualTo(7));
            Assert.That(county.DistrictIds, Has.Count.EqualTo(6));
            Assert.That(county.DistrictIds, Does.Contain("blackridge-foundry-row"));
        }

        [Test]
        public void SanTrianaCountyHasDistrictsButHonestlyNoFacilities()
        {
            // The research corpus names San Triana's zones but never
            // names any facility against that county -- FacilityIds
            // should stay empty rather than guess.
            var county = WorldContent.SanTrianaCounty();
            Assert.That(county.DistrictIds, Has.Count.EqualTo(5));
            Assert.That(county.FacilityIds, Is.Empty);
        }

        [Test]
        public void AiVehicleSessionCanFollowEveryNewTrackLineWithoutError()
        {
            // Real integration check, same pattern as
            // TrackAiDriverCanFollowTheFoundryRowLineAllTheWayAround, run
            // against all 6 new lines to confirm TrackAiDriver.Perceive
            // never returns null and every node is reachable in sequence
            // for shapes it has never seen before (in particular the
            // procedurally-generated oval loops).
            System.Collections.Generic.IReadOnlyList<WTRL.Racing.TrackNode>[] lines =
            {
                RacingContent.CutbackTriOvalLine().Nodes,
                RacingContent.LongbowSpeedwayLine().Nodes,
                RacingContent.HighbankSuperspeedwayLine().Nodes,
                RacingContent.WhisperwoodForestCircuitLine().Nodes,
                RacingContent.CliffsideCoastalCircuitLine().Nodes,
                RacingContent.IroncladTechnicalCircuitLine().Nodes,
            };

            foreach (var nodes in lines)
            {
                var line = new WTRL.Racing.TrackLineDefinition("test-line", "test-track", nodes);
                var state = VehicleSimState.Default();
                state.X = nodes[0].X;
                state.Z = nodes[0].Z;

                var visited = new System.Collections.Generic.HashSet<int>();
                for (var step = 0; step < 2000 && visited.Count < nodes.Count; step++)
                {
                    var perception = TrackAiDriver.Perceive(state, line);
                    Assert.That(perception, Is.Not.Null);
                    visited.Add(perception!.Value.NodeIndex);

                    var target = nodes[perception.Value.NodeIndex];
                    var dx = target.X - state.X;
                    var dz = target.Z - state.Z;
                    var dist = System.Math.Sqrt(dx * dx + dz * dz);
                    if (dist > 0.01)
                    {
                        state.X += dx / dist * 5;
                        state.Z += dz / dist * 5;
                    }
                }

                Assert.That(visited, Has.Count.EqualTo(nodes.Count));
            }
        }

        [Test]
        public void TrackAiDriverCanFollowTheFoundryRowLineAllTheWayAround()
        {
            // Real integration check: drives TrackAiDriver.Perceive/Input
            // (existing, previously only fixture-tested) against this
            // pass's real authored content, confirming the two closed-
            // loop-shaped systems actually fit together -- every node is
            // reachable in sequence and perception never returns null.
            var line = RacingContent.FoundryRowCircuitLine();
            var model = new DriverModel
            {
                Aggression = 0.5,
                Consistency = 0.8,
                BrakingConfidence = 0.7,
                ThrottleDiscipline = 0.8,
            };

            var state = VehicleSimState.Default();
            state.X = line.Nodes[0].X;
            state.Z = line.Nodes[0].Z;

            var visitedNodes = new System.Collections.Generic.HashSet<int>();
            for (var step = 0; step < 500 && visitedNodes.Count < line.Nodes.Count; step++)
            {
                var perception = TrackAiDriver.Perceive(state, line);
                Assert.That(perception, Is.Not.Null);
                visitedNodes.Add(perception!.Value.NodeIndex);

                var input = TrackAiDriver.Input(model, perception.Value, currentSpeed: 20);
                Assert.That(input.Throttle, Is.InRange(-1.0, 1.0));

                // Move the vehicle a fixed step toward its target node --
                // a simplified kinematic stand-in, not a physics
                // integration (WTRL.Vehicle's own tests already cover
                // real physics; this test is about the AI/track content
                // fitting together, not re-deriving VehicleSimulation.Step).
                var target = line.Nodes[perception.Value.NodeIndex];
                var dx = target.X - state.X;
                var dz = target.Z - state.Z;
                var dist = System.Math.Sqrt(dx * dx + dz * dz);
                if (dist > 0.01)
                {
                    state.X += dx / dist * 5;
                    state.Z += dz / dist * 5;
                }
            }

            Assert.That(visitedNodes, Has.Count.EqualTo(line.Nodes.Count),
                "the AI driver should reach every node on the Foundry Row circuit in sequence");
        }
    }
}
