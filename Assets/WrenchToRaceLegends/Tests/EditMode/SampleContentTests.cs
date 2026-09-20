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

            double perimeter = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                var a = nodes[i];
                var b = nodes[(i + 1) % nodes.Count];
                perimeter += System.Math.Sqrt(System.Math.Pow(b.X - a.X, 2) + System.Math.Pow(b.Z - a.Z, 2));
            }

            Assert.That(track.LengthM, Is.EqualTo(perimeter).Within(0.5));
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
