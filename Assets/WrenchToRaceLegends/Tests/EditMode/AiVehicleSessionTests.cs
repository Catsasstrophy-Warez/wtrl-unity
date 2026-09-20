using System.Collections.Generic;
using NUnit.Framework;
using WTRL.Racing;
using WTRL.Vehicle;
using RacingContent = WTRL.Racing.SampleContent;

namespace WTRL.Tests
{
    /// <summary>New tests for AiVehicleSession -- no Swift-test
    /// equivalent (this type didn't exist there). Uses the same
    /// Marsh-gen1-equivalent fixture values as MarshContentBuilder.cs
    /// (kept independent rather than importing the .asset, same
    /// discipline as every other test's Make*() helpers) driving the
    /// real Foundry Row circuit content from SampleContentTests. This is
    /// the first test in the project exercising real
    /// VehicleSimulation.Step physics under AI control end to end,
    /// rather than a simplified kinematic stand-in
    /// (SampleContentTests) or player input
    /// (WTRL.Tests.VehicleRuntimeControllerTests). Run via the same
    /// throwaway dotnet test project as every other assembly -- all
    /// pass.</summary>
    public class AiVehicleSessionTests
    {
        private static VehicleDefinition MakeMarshVehicle() =>
            new("marsh-gen1", "marsh_gen1_1991", "Marsh Constant Gen 1", massKg: 1365, wheelbaseM: 2.53,
                engineId: "marsh-gen1-engine", transmissionId: "marsh-gen1-gearbox", suspensionId: "marsh-gen1-suspension");

        private static EngineDefinition MakeMarshEngine() =>
            new("marsh-gen1-engine", "Marsh Gen1 V6", displacementLiters: 3.0, peakPowerHp: 270, peakTorqueLbFt: 210)
            {
                RedlineRpm = 8000,
                IdleRpm = 900,
            };

        private static TransmissionDefinition MakeMarshTransmission() =>
            new("marsh-gen1-gearbox", "Marsh Gen1 5-Speed", new[] { 3.23, 2.05, 1.48, 1.15, 0.91 }, finalDrive: 4.06);

        private static SuspensionDefinition MakeMarshSuspension() =>
            new("marsh-gen1-suspension", "Marsh Gen1 Suspension", "double-wishbone", "double-wishbone");

        private static TireDefinition MakeMarshTire() =>
            new("marsh-gen1-tire", "Marsh Gen1 Sport Tire", longitudinalStiffness: 9.0, corneringStiffness: 6.2,
                peakSlipRatio: 0.11, peakSlipAngleRadians: 0.10);

        private static DriverModel MakeDriverModel() => new()
        {
            Aggression = 0.5,
            Consistency = 0.8,
            BrakingConfidence = 0.7,
            ThrottleDiscipline = 0.8,
            WetSkill = 0.6,
            TireConservation = 0.6,
            MechanicalSympathy = 0.6,
            MistakeProbability = 0,
        };

        private static AiVehicleSession MakeSession() => new(MakeDriverModel(), RacingContent.FoundryRowCircuitLine(),
            MakeMarshVehicle(), MakeMarshEngine(), MakeMarshTransmission(), MakeMarshTire(), MakeMarshSuspension());

        [Test]
        public void AiVehicleSessionDrivesRealPhysicsAroundTheFoundryRowCircuit()
        {
            var session = MakeSession();
            var startPosition = (session.State.X, session.State.Z);
            var visitedNodes = new HashSet<int>();

            // 3600 steps at 1/60s is 60 simulated seconds -- comfortably
            // enough to complete this ~633m circuit at the line's
            // 18-45 m/s target speeds (worst case ~35s per lap).
            for (var i = 0; i < 3600 && visitedNodes.Count < RacingContent.FoundryRowCircuitLine().Nodes.Count; i++)
            {
                session.Step(1.0 / 60);
                if (session.LastPerception != null) visitedNodes.Add(session.LastPerception.Value.NodeIndex);
            }

            Assert.That(visitedNodes, Has.Count.EqualTo(RacingContent.FoundryRowCircuitLine().Nodes.Count),
                "the AI-driven vehicle should reach every node on the circuit under real physics");

            var moved = System.Math.Sqrt(
                System.Math.Pow(session.State.X - startPosition.X, 2) +
                System.Math.Pow(session.State.Z - startPosition.Z, 2));
            Assert.That(moved, Is.GreaterThan(0), "the vehicle should have moved from its starting position");
        }

        [Test]
        public void AiVehicleSessionProducesFiniteStateThroughoutARun()
        {
            // Guards against the class of determinism/stability bug
            // WTRL.Vehicle's own DeterministicFuzzInputs_RemainFinite
            // test guards for the underlying physics -- this confirms
            // AI-generated input doesn't drive the sim into NaN/Infinity
            // either, over a longer run than the single-lap test above.
            var session = MakeSession();
            for (var i = 0; i < 7200; i++)
            {
                session.Step(1.0 / 60);
                Assert.That(double.IsFinite(session.State.X), Is.True);
                Assert.That(double.IsFinite(session.State.Z), Is.True);
                Assert.That(double.IsFinite(session.State.SpeedMps), Is.True);
            }
        }

        [Test]
        public void EmptyTrackLineLeavesTheVehicleStationary()
        {
            var session = new AiVehicleSession(MakeDriverModel(),
                new TrackLineDefinition("empty", "nowhere", System.Array.Empty<TrackNode>()),
                MakeMarshVehicle(), MakeMarshEngine(), MakeMarshTransmission(), MakeMarshTire(), MakeMarshSuspension());

            var start = (session.State.X, session.State.Z);
            session.Step(1.0 / 60);

            Assert.That(session.State.X, Is.EqualTo(start.X));
            Assert.That(session.State.Z, Is.EqualTo(start.Z));
            Assert.That(session.LastPerception, Is.Null);
        }
    }
}
